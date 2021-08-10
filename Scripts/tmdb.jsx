
//get data from a web resource/api
async function getData(url = '', headers = { 'Content-Type': 'application/json' /*,'Content-Type': 'application/x-www-form-urlencoded'*/ }, responseType = 'json') {
    // Default options are marked with *
    const response = await fetch(url, {
        method: 'GET', // *GET, POST, PUT, DELETE, etc.
        mode: 'cors', // no-cors, *cors, same-origin
        cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
        credentials: 'same-origin', // include, *same-origin, omit
        headers: headers,
        redirect: 'follow', // manual, *follow, error
        referrerPolicy: 'no-referrer' // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
        //body: JSON.stringify(data) // body data type must match "Content-Type" header
    });

    if (responseType == 'blob')
        return response.blob();
    return response.json(); 
}

//convert arrayBuffer to base64
function _arrayBufferToBase64(buffer) {
    var binary = '';
    var bytes = new Uint8Array(buffer);
    var len = bytes.byteLength;
    for (var i = 0; i < len; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
}

api_key = "37e0222683173a81e04881c588f4951c";
base_url = "https://api.themoviedb.org/3";
image_url = "https://image.tmdb.org";

class MovieForm extends React.Component {
    constructor(props) {
        super(props);

        //set state to initial values
        this.state = {
            selected: null,
            data: [{ id: null, name: "Click Search" }],
            error: null,
            isLoaded: false,
            isEdit: document.getElementById("orig_name").value != '',
            formcontent: {
                name: document.getElementById("orig_name").value,
                desc: document.getElementById("orig_desc").value,
                img_src: "data:image/png;base64," + document.getElementById("orig_image").value
            }
        };

        this.handleChange = this.handleChange.bind(this);
        this.handleDescChange = this.handleDescChange.bind(this);
        this.handleNameChange = this.handleNameChange.bind(this);
        this.searchTMDB = this.searchTMDB.bind(this);
        this.populateSelect = this.populateSelect.bind(this);
        this.reset = this.reset.bind(this);
        this.fileLoaded = this.fileLoaded.bind(this);
    }
    //reset click handler
    reset(event) {
        this.setState({
            selected: null,
            data: [{ id: null, name: "Click Search" }],
            error: null,
            isLoaded: false,
            isEdit: document.getElementById("orig_name").value != '',
            formcontent: {
                name: document.getElementById("orig_name").value,
                desc: document.getElementById("orig_desc").value,
                img_src: "data:image/png;base64," + document.getElementById("orig_image").value
            }
        });
        this.setFile(null, null);
    }

    //handle change to the file upload user control, populate the image from the file
    fileLoaded(event) {        
        var urlCreator = window.URL || window.webkitURL;
        var imageUrl = urlCreator.createObjectURL(event.target.files[0]);
        this.state.formcontent.img_src = imageUrl;
        this.setState(this.state);
    }

    //populate the file input with the image data from TMDB api 
    setFile(blob, file_name) {
        let fileInputElement = document.getElementById('image_file');
        let container = new DataTransfer();

        if (blob != null) {
            let data = blob;
            let file = new File([data], file_name, { type: "image/png", lastModified: new Date().getTime() });
            container.items.add(file);
        }

        fileInputElement.files = container.files;    
    }
    //update the description
    handleDescChange(event) {
        if (this.state.formcontent.desc == event.target.value)
            return;
        this.state.formcontent.desc = event.target.value;
        this.setState(this.state);
    }
    //update the name
    handleNameChange(event) {
        if (this.state.formcontent.name == event.target.value && !this.state.isLoaded)
            return;
        this.state.formcontent.name = event.target.value;
        this.state.isLoaded = false;
        this.setState(this.state);
    }    
    //update the selected item in the TMDB search results
    handleChange(event) {
        this.state.selected = event.target.value;
    }    

    //Search the TMDB api for the name of the movie on the user form
    searchTMDB(event) {
        try {
            getData(base_url + "/search/movie" + "?api_key=" + api_key + "&query=" + document.getElementById('name').value)
                .then(
                    (result) => {
                        if (result.results.length > 0)
                            this.setState({
                                isLoaded: true,
                                data: result.results.map(
                                    (item) => {
                                        return {
                                            id: item.id,
                                            name: item.original_title
                                        };
                                    }),
                                selected: result.results[0].id,
                                formcontent: this.state.formcontent
                            });
                        else {
                            this.setState({
                                data: [{ id: null, name: "No results. Try again." }],
                                isLoaded: true,
                                formcontent: this.state.formcontent
                            });
                        }
                    }
                    ,
                    (error) => this.setState(
                        {
                            isLoaded: false,
                            error: error,
                            formcontent: this.state.formcontent
                        }
                    )
                );
        }
        catch(e)
        {
            console.log(e);
        }
    }

    //Populate the form with data from TMDB api
    populateSelect(event) {
        try {
            getData(base_url + "/movie/" + this.state.selected + "?api_key=" + api_key)
                .then(
                    (result) => {
                        if (result != undefined) {
                            this.state.formcontent.name = result.original_title;
                            if (result.overview && result.overview != '')
                                this.state.formcontent.desc = result.overview;
                            else
                                this.state.formcontent.desc = "No overview provided.";

                            getData(image_url + "/t/p/w500" + result.poster_path, {}, 'blob')
                                .then(

                                    (result2) => {
                                        this.setFile(result2, result.poster_path);
                                        var urlCreator = window.URL || window.webkitURL;
                                        var imageUrl = urlCreator.createObjectURL(result2);
                                        this.state.formcontent.img_src = imageUrl;
                                        this.setState(this.state);
                                    },

                                    (error) => {
                                        this.state.formcontent.img_src = '';
                                        this.state.error = error;
                                        this.setState(this.state);
                                    }
                                );

                        }
                        else {
                            throw (new Exception("Error: no movie details found from TMDB."));
                        }
                    }
                    ,
                    (error) => { this.state.error = error; }
                );
        }
        catch (e) {
            console.log(e);
        }
    }
        
    //Render the movie form and the TMDB user controls
    render() {
        return (
            <div>
                <div className="col-xs-7 col-sm-6 col-md-5 col-lg-4">
                    Name:
                    <p>
                        <input className="form-control" type="text" placeholder="Name" name="Name" id="name" value={this.state.formcontent.name} onChange={this.handleNameChange} required />
                    </p>
                    Movie Poster Image:
                    <p>
                        <img className="img-responsive" id="image_poster" alt="No Poster Image." src={this.state.formcontent.img_src} />
                        <input type="file" name="image_file" id="image_file" onChange={this.fileLoaded} required={ !this.state.isEdit }/>
                    </p>
                    Description:
                    <p>
                        <textarea className="form-control" rows="8" cols="60" placeholder="Description" name="Description" id="description" value={this.state.formcontent.desc} onChange={ this.handleDescChange } required></textarea>
                    </p>
                    <p>
                        <input type="submit" value="Add/Update" className="btn btn-primary btn-lg" />
                        <input type="button" value="Reset" onClick={this.reset} className="btn btn-outline-seconday btn-lg" />
                    </p>
                </div>
                <div className="col-xs-5 col-sm-6">
                    <div className="tmdb">
                        Optional "The Movie Database" lookup:                    
                        <p>                        
                            <button type="button" onClick={this.searchTMDB} disabled={this.state.formcontent.name == ''} data-toggle="tooltip" data-placement="top"
                                title={this.state.formcontent.name == '' ? 'Please enter a movie name.' : "Search: " + this.state.formcontent.name}>Search TMDB!</button>
                            <select onChange={this.handleChange} hidden={!this.state.isLoaded}>
                                {this.state.data.map((movie) =>
                                    <option key={movie.id} value={movie.id}>
                                        {movie.name}
                                    </option>
                                )}
                            </select>
                            <button type="button" onClick={this.populateSelect} hidden={!this.state.isLoaded || this.state.selected == null} data-toggle="tooltip" data-placement="top"
                                title={this.state.selected == null ? '<b>Please select a movie.</b>' : "Populate selected data from TMDB"}>Populate</button>
                        </p>
                        <p>
                            Powered by:
                        </p>
                        <img className="col-xs-1 col-md-4" src={window.location.origin + "/Content/blue_square_2.svg"}></img>
                    </div>
                </div>
            </div>
        );
    }
}

ReactDOM.render(
    <MovieForm />,
    document.getElementById('react_form')
);
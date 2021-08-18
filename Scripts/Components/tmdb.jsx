class TheMDB extends React.Component {
    constructor(props) {
        super(props);

        //set state to initial values
        this.state = {
            selected: null,
            data: [{ id: null, name: "Click Search" }],
            isLoaded: false
        };

        this.handleChange = this.handleChange.bind(this);
        this.searchTMDB = this.searchTMDB.bind(this);
        this.populateSelect = this.populateSelect.bind(this);
    }

    //get data from a web resource/api
    async getData(url = '', headers = { 'Content-Type': 'application/json' /*,'Content-Type': 'application/x-www-form-urlencoded'*/ }, responseType = 'json') {
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

    //update the selected item in the TMDB search results
    handleChange(event) {
        this.state.selected = event.target.value;
    }    

    //Search the TMDB api for the name of the movie on the user form
    async searchTMDB(event) {
        let result = '';
        try {
            result = await this.getData(this.props.base_url + "/search/movie" + "?api_key=" + this.props.api_key + "&query=" + this.props.searchTerm);

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
                    selected: result.results[0].id
                });
            else {
                this.setState({
                    data: [{ id: null, name: "No results. Try again." }],
                    isLoaded: true
                });
            }
        }
        catch (e) {
            this.setState(
                {
                    isLoaded: false,
                    error: result
                }
            );
            console.log(e);
        }
    }

    //call Parent setFile handler
    setFile(blob, filename) {
        this.props.callSetFile(blob, filename);
    }

    //Populate the form with data from TMDB api
    async populateSelect(event) {
        let newFormContent = {};
        let result,result2 = '';
        try {
            result = await this.getData(this.props.base_url + "/movie/" + this.state.selected + "?api_key=" + this.props.api_key);
            newFormContent.name = result.original_title;
            if (result.overview && result.overview != '')
                newFormContent.desc = result.overview;
            else
                newFormContent.desc = "No overview provided.";

            try {
                result2 = await this.getData(this.props.image_url + "/t/p/" + this.props.image_size + result.poster_path, {}, 'blob');

                this.setFile(result2, result.poster_path);
                var urlCreator = window.URL || window.webkitURL;
                var imageUrl = urlCreator.createObjectURL(result2);
                newFormContent.img_src = imageUrl;
            }
            catch (e) {
                this.setState(
                    {
                        error: result2
                    }
                );
            }
            finally
            {
                //populate parent
                this.props.onPopulate(newFormContent);
            }
        }
        catch (e) {
            this.setState(
                {
                    error: result
                }
            );
            console.log(e);
        }
    }
        
    //Render the movie form and the TMDB user controls
    render() {
        return (
            <div className="col-xs-5 col-sm-6">
                <div className="tmdb">
                    Optional "The Movie Database" lookup:                    
                    <p>                        
                        <button type="button" onClick={this.searchTMDB} disabled={this.props.searchTerm == ''} data-toggle="tooltip" data-placement="top"
                            title={this.props.searchTerm == '' ? 'Please enter a movie name.' : "Search: " + this.props.searchTerm}>Search TMDB!</button>
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
        );
    }
}
class MovieInput extends React.Component {
    constructor(props) {
        super(props);

        //set the SetFile function ptr in parent
        this.props.setSetFile(this.setFile);

        this.handleDescChange = this.handleDescChange.bind(this);
        this.handleNameChange = this.handleNameChange.bind(this);

        this.reset = this.reset.bind(this);
        this.fileLoaded = this.fileLoaded.bind(this);
    }

    //reset click handler
    reset(event) {
        this.setState({
            formcontent: this.props.initialFormContent
        });
        this.setFile(null, null);
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

    //handle change to the file upload user control, populate the image from the file
    fileLoaded(event) {        
        var urlCreator = window.URL || window.webkitURL;
        var imageUrl = urlCreator.createObjectURL(event.target.files[0]);


        //call function pointer to update
        this.props.onChangeForm({
            img_src: imageUrl
        });
    }

    //update the description
    handleDescChange(event) {
        //update the state
        if (this.props.formcontent.desc == event.target.value)
            return;

        //call function pointer to update
        this.props.onChangeForm({
            desc: event.target.value
        });
    }

    //update the name
    handleNameChange(event) {
        //update the state
        if (this.props.formcontent.name == event.target.value)
            return;

        //call function pointer to update
        this.props.onChangeForm({
            name: event.target.value
        });
    }
            
    //Render the movie form and the TMDB user controls
    render() {
        return (
            <div className="col-xs-7 col-sm-6 col-md-5 col-lg-4">
                Name:
                <p>
                    <input className="form-control" type="text" placeholder="Name" name="Name" id="name" value={this.props.formcontent.name} onChange={this.handleNameChange} required />
                </p>
                Movie Poster Image:
                <p>
                    <img className="img-responsive" id="image_poster" alt="No Poster Image." src={this.props.formcontent.img_src} required={this.props.isEdit}/>
                    <input type="file" name="image_file" id="image_file" onChange={this.fileLoaded} required={!this.props.isEdit} />
                </p>
                Description:
                <p>
                    <textarea className="form-control" rows="8" cols="60" placeholder="Description" name="Description" id="description" value={this.props.formcontent.desc} onChange={ this.handleDescChange } required></textarea>
                </p>
                <p>
                    <input type="submit" value="Add/Update" className="btn btn-primary btn-lg" />
                    <input type="button" value="Reset" onClick={this.reset} className="btn btn-outline-seconday btn-lg" />
                </p>
            </div>
        );
    }
}
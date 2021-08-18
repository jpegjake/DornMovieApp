

class MovieForm extends React.Component {
    constructor(props) {
        super(props);

        //set state to initial values
        this.state = {
            selected: null,
            data: [{ id: null, name: "Click Search" }],
            error: null,
            isLoaded: false,
            formcontent: {
                name: document.getElementById("orig_name").value,
                desc: document.getElementById("orig_desc").value,
                img_src: "data:image/png;base64," + document.getElementById("orig_image").value
            }
        };
    }

    onChangeForm(formcontent) {
        if (formcontent.img_src === undefined)
            formcontent.img_src = this.state.formcontent.img_src;
        this.setState({
            formcontent: formcontent
        });
    }

    setSetFile(funcptr) {
        this.setState({
            setFile: funcptr
        });
    }
    callSetFile(blob,filename) {
        this.state.setFile(blob, filename);
    }
            
    //Render the movie form and the TMDB user controls
    render() {
        return (
            <div>
                <MovieInput
                    onChangeForm={this.onChangeForm.bind(this)}
                    formcontent={this.state.formcontent}
                    isEdit={document.getElementById("orig_name").value != ''}
                    setSetFile={this.setSetFile.bind(this)} />
                <TheMDB
                    callSetFile={this.callSetFile.bind(this)}
                    onPopulate={this.onChangeForm.bind(this)}
                    searchTerm={this.state.formcontent.name}
                    base_url={base_url}
                    api_key={api_key}
                    image_url={image_url}
                    image_size="w500" />
            </div>
        );
    }
}

ReactDOM.render(
    <MovieForm />,
    document.getElementById('react_form')
);
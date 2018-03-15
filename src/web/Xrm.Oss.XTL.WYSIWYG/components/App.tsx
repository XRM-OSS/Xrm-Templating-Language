import * as React from "react";
import { Well, ButtonToolbar, ButtonGroup, Button, DropdownButton, MenuItem } from "react-bootstrap";

interface WYSIWYGEditorState {
  inputTemplate: string;
  selectedEntityLogicalName: string;
  selectedEntityId: string;
  selectedEntityName: string;
  resultText: string;
  traceLog: string;
  error: string;
  success: boolean;
}

export default class WYSIWYGEditor extends React.PureComponent<any, WYSIWYGEditorState> {
    private WebApiClient: any;

    constructor(props: any) {
        super(props);

        this.state = {
          selectedEntityLogicalName: "",
          selectedEntityName: "",
          selectedEntityId: "",
          inputTemplate: "",
          resultText: "",
          traceLog: "",
          error: "",
          success: true
        };

        this.WebApiClient = (window as any).WebApiClient;

        this.inputChanged = this.inputChanged.bind(this);
        this.preview = this.preview.bind(this);
        this.selectTarget = this.selectTarget.bind(this);
    }

    preview(e: any) {
        const request = this.WebApiClient.Requests.Request.prototype.with({
            method: "POST",
            name: "oss_XTLProcessTemplate",
            bound: false
        });

        this.WebApiClient.Execute(request.with({
            payload: {
                jsonInput: JSON.stringify({
                    target: {
                        Id: this.state.selectedEntityId,
                        LogicalName: this.state.selectedEntityLogicalName
                    },
                    template: this.state.inputTemplate
                })
            }
        }))
        .then((result: any) => {
            const json = JSON.parse(result.jsonOutput);

            this.setState({
                resultText: (json.result || "").replace(/\\n/g, "\r\n"),
                traceLog: json.traceLog
            });
        });
    }

    selectTarget(e: any) {
        const url = this.WebApiClient.GetApiUrl().replace("/api/data/v8.0/", "") + "/_controls/lookup/lookupinfo.aspx?AllowFilterOff=1&DefaultType=1&DisableQuickFind=0&DisableViewPicker=0&LookupStyle=single&ShowNewButton=0&ShowPropButton=0&browse=false&objecttypes=1";
        const Xrm: any = (window as any).Xrm;
        Xrm.Internal.openDialog(url , {width: 300, height: 500}, undefined, undefined, (result: any) => {
            const reference = result.items[0];

            this.setState({
                selectedEntityId: reference.id,
                selectedEntityLogicalName: reference.typename,
                selectedEntityName: reference.name
            });
        });
    }

    inputChanged(e: any) {
      this.setState({
        inputTemplate: e.target.value
      });
    }

    render() {
        return (
        <div>
          {this.state.selectedEntityId && <a>Entity: {this.state.selectedEntityLogicalName}, Id: {this.state.selectedEntityId}, Name: {this.state.selectedEntityName}</a>}
          {!this.state.success && <a>"Error: {this.state.error}</a>}
          <div>
            <ButtonToolbar>
              <ButtonGroup>
                <DropdownButton
                    bsStyle="default"
                    title="Entity"
                    id="EntitySelect"
                >
                      { [1, 2, 3, 4].map( value => <MenuItem eventKey={value}>{value}</MenuItem> ) }
                </DropdownButton>
                <Button bsStyle="default" onClick={ this.selectTarget }>Select Target</Button>
                <Button bsStyle="default" onClick={ this.preview }>Preview</Button>
              </ButtonGroup>
            </ButtonToolbar>
              <textarea className="col-xs-6" style={ { "height": "100vh" } } onChange={ this.inputChanged } />
              <textarea className="col-xs-6" style={ { "height": "50vh" } } value={ this.state.resultText } disabled />
              <textarea className="col-xs-6" style={ { "height": "50vh" } } value={ this.state.traceLog } disabled />
          </div>
        </div>
        );
    }
}

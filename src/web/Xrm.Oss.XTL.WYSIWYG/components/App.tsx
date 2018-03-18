import * as React from "react";
import { WebApiClient } from "xrm-webapi-client";
import { Well, ButtonToolbar, ButtonGroup, Button, DropdownButton, MenuItem, Modal } from "react-bootstrap";

interface WYSIWYGEditorState {
  inputTemplate: string;
  selectedEntityLogicalName: string;
  selectedEntityId: string;
  selectedEntityName: string;
  resultText: string;
  traceLog: string;
  error: string;
  success: boolean;
  entities: Array<any>;
  selectedTypeCode: number;
  requestPending: boolean;
}

export default class WYSIWYGEditor extends React.PureComponent<any, WYSIWYGEditorState> {
    private WebApiClient: typeof WebApiClient;

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
          success: true,
          entities: [],
          selectedTypeCode: 0,
          requestPending: false
        };

        // Webpack should import WebApiClient from global itself, but somehow it doesn't
        this.WebApiClient = (window as any).WebApiClient;

        this.inputChanged = this.inputChanged.bind(this);
        this.preview = this.preview.bind(this);
        this.selectTarget = this.selectTarget.bind(this);
        this.setTypeCode = this.setTypeCode.bind(this);
    }

    componentDidMount() {
        this.WebApiClient.Retrieve({entityName: "EntityDefinition", queryParams: "?$select=ObjectTypeCode,SchemaName&$filter=IsValidForAdvancedFind eq true"})
            .then((result: any) => {
                this.setState({
                    entities: (result.value as Array<any>).filter(e => e.SchemaName).sort((e1, e2) => e1.SchemaName >= e2.SchemaName ? 1 : -1)
                });
            });
    }

    preview(e: any) {
        this.setState({
            requestPending: true
        });

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
                requestPending: false,
                resultText: (json.result || "").replace(/\\n/g, "\r\n"),
                traceLog: json.traceLog
            });
        });
    }

    selectTarget(e: any) {
        const url = this.WebApiClient.GetApiUrl().replace("/api/data/v8.0/", "") + `/_controls/lookup/lookupinfo.aspx?AllowFilterOff=1&DisableQuickFind=0&DisableViewPicker=0&LookupStyle=single&ShowNewButton=0&ShowPropButton=0&browse=false&objecttypes=${this.state.selectedTypeCode}`;
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

    setTypeCode(eventKey: any) {
        this.setState({
            selectedTypeCode: parseInt(eventKey)
        });
    }

    render() {
        return (
        <div>
          {this.state.requestPending &&
            <Modal.Dialog>
            <Modal.Header>
              <Modal.Title>Processing Request</Modal.Title>
            </Modal.Header>

            <Modal.Body>Your template is being processed on the server...</Modal.Body>
          </Modal.Dialog>}
          {this.state.selectedEntityId && <a>Entity: {this.state.selectedEntityLogicalName}, Id: {this.state.selectedEntityId}, Name: {this.state.selectedEntityName}</a>}
          {!this.state.success && <a>"Error: {this.state.error}</a>}
          <div>
            <ButtonToolbar>
              <ButtonGroup>
                <DropdownButton
                    bsStyle="default"
                    title={this.state.selectedTypeCode ? this.state.entities.find(e => e.ObjectTypeCode == this.state.selectedTypeCode).SchemaName : "Entity" }
                    id="EntitySelect"
                >
                      { this.state.entities.map( value => <MenuItem onSelect={this.setTypeCode} eventKey={value.ObjectTypeCode}>{value.SchemaName}</MenuItem> ) }
                </DropdownButton>
                <Button bsStyle="default" disabled={this.state.selectedTypeCode === 0} onClick={ this.selectTarget }>Select Target</Button>
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

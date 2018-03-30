import * as React from "react";
import WebApiClient from "xrm-webapi-client";
import { Well, ButtonToolbar, ButtonGroup, Button, DropdownButton, MenuItem, Modal, FormGroup, ControlLabel, FormControl } from "react-bootstrap";
import * as Parser from "html-react-parser";

interface WYSIWYGEditorState {
  inputTemplate: string;
  executionCriteria: string;
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
  copyTemplate: boolean;
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
          executionCriteria: "",
          resultText: "",
          traceLog: "",
          error: "",
          success: true,
          entities: [],
          selectedTypeCode: 0,
          requestPending: false,
          copyTemplate: false
        };

        // Webpack should import WebApiClient from global itself, but somehow it doesn't
        this.WebApiClient = (window as any).WebApiClient;

        this.inputChanged = this.inputChanged.bind(this);
        this.criteriaChanged = this.criteriaChanged.bind(this);
        this.preview = this.preview.bind(this);
        this.selectTarget = this.selectTarget.bind(this);
        this.setTypeCode = this.setTypeCode.bind(this);
        this.copy = this.copy.bind(this);
        this.closeCopyDialog = this.closeCopyDialog.bind(this);
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
                    template: this.state.inputTemplate,
                    executionCriteria: this.state.executionCriteria
                })
            }
        }))
        .then((result: any) => {
            const json = JSON.parse(result.jsonOutput);

            this.setState({
                requestPending: false,
                resultText: (json.result || "").replace(/\n/g, "<br />"),
                traceLog: json.traceLog
            });
        });
    }

    selectTarget(e: any) {
        const url = this.WebApiClient.GetApiUrl().replace("/api/data/v8.0/", "") + `/_controls/lookup/lookupinfo.aspx?AllowFilterOff=1&DisableQuickFind=0&DisableViewPicker=0&LookupStyle=single&ShowNewButton=0&ShowPropButton=0&browse=false&objecttypes=${this.state.selectedTypeCode}`;
        const Xrm: any = (window as any).Xrm;
        const options = new Xrm.DialogOptions();
        options.width = 500;
        options.height = 800;

        Xrm.Internal.openDialog(url , options, undefined, undefined, (result: any) => {
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

    criteriaChanged(e: any) {
      this.setState({
        executionCriteria: e.target.value
      });
    }

    setTypeCode(eventKey: any) {
        this.setState({
            selectedTypeCode: parseInt(eventKey)
        });
    }

    copy() {
        this.setState({
            copyTemplate: true
        });
    }

    closeCopyDialog() {
        this.setState({
            copyTemplate: false
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
          {this.state.copyTemplate &&
            <Modal.Dialog>
            <Modal.Header>
              <Modal.Title>Copy Json below</Modal.Title>
            </Modal.Header>

            <Modal.Body>{JSON.stringify({template: this.state.inputTemplate, executionCriteria: this.state.executionCriteria})}</Modal.Body>
            <Modal.Footer>
                <Button bsStyle="default" onClick={ this.closeCopyDialog }>Close</Button>
            </Modal.Footer>
          </Modal.Dialog>}
          {this.state.selectedEntityId && <a>Entity: {this.state.selectedEntityLogicalName}, Id: {this.state.selectedEntityId}, Name: {this.state.selectedEntityName}</a>}
          {!this.state.success && <a>"Error: {this.state.error}</a>}
          <div>
            <ButtonToolbar style={{"padding-bottom": "10px"}}>
              <ButtonGroup>
                <DropdownButton
                    bsStyle="default"
                    title={this.state.selectedTypeCode ? this.state.entities.find(e => e.ObjectTypeCode == this.state.selectedTypeCode).SchemaName : "Entity" }
                    id="EntitySelect"
                >
                      { this.state.entities.map( value => <MenuItem onSelect={this.setTypeCode} eventKey={value.ObjectTypeCode}>{value.SchemaName}</MenuItem> ) }
                </DropdownButton>
                <Button bsStyle="default" disabled={this.state.selectedTypeCode === 0} onClick={ this.selectTarget }>Select Target</Button>
                <Button bsStyle="default" disabled={!this.state.selectedEntityId || !this.state.selectedTypeCode} onClick={ this.preview }>Preview</Button>
                <Button bsStyle="default" onClick={ this.copy }>Copy Current Template</Button>
              </ButtonGroup>
            </ButtonToolbar>
              <FormGroup className="col-xs-6" controlId="input">
                <ControlLabel>Execution Criteria</ControlLabel>
                <FormControl style={ { "height": "25vh", "overflow": "auto" } } onChange={ this.criteriaChanged } componentClass="textarea" placeholder="Leave empty for executing unconditionally" />
                <ControlLabel style={{"padding-top": "10px"}}>Template</ControlLabel>
                <FormControl style={ { "height": "75vh", "overflow": "auto" } } onChange={ this.inputChanged } componentClass="textarea" placeholder="Enter template" />
              </FormGroup>
              <div className="col-xs-6">
                <ControlLabel>Result</ControlLabel>
                <div style={ { "height": "50vh", "border": "1px solid lightgray", "overflow": "auto" } }>{Parser(this.state.resultText)}</div>
                <FormGroup controlId="output">
                  <ControlLabel style={{"padding-top": "10px"}}>Interpreter Trace</ControlLabel>
                  <FormControl style={ { "height": "50vh", "overflow": "auto" } } componentClass="textarea" value={ this.state.traceLog } disabled />
                </FormGroup>
              </div>
          </div>
        </div>
        );
    }
}

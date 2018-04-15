import * as React from "react";
import { EntityDefinition } from "../domain/EntityDefinition";
import WebApiClient from "xrm-webapi-client";
import { StepCreationDialog } from "./StepCreationDialog";
import { SdkStep } from "../domain/SdkStep";
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
  entities: Array<EntityDefinition>;
  selectedTypeCode: number;
  requestPending: boolean;
  copyTemplate: boolean;
  pluginType: any;
  showSdkSteps: boolean;
  selectedSdkStep: SdkStep;
  showSdkStepCreationDialog: boolean;
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
          copyTemplate: false,
          pluginType: undefined,
          showSdkSteps: false,
          selectedSdkStep: undefined,
          showSdkStepCreationDialog: false
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
        this.showSdkSteps = this.showSdkSteps.bind(this);
        this.setSelectedSdkStep = this.setSelectedSdkStep.bind(this);
        this.saveSelectedSdkStep = this.saveSelectedSdkStep.bind(this);
        this.createNewSdkStep = this.createNewSdkStep.bind(this);
        this.sdkStepCreationHandler = this.sdkStepCreationHandler.bind(this);
        this.reportError = this.reportError.bind(this);
    }

    componentDidMount() {
        this.WebApiClient.Retrieve({entityName: "EntityDefinition", queryParams: "?$select=ObjectTypeCode,SchemaName,LogicalName&$filter=IsValidForAdvancedFind eq true"})
            .then((result: any) => {
                this.setState({
                    entities: (result.value as Array<any>).filter(e => e.SchemaName).sort((e1, e2) => e1.SchemaName >= e2.SchemaName ? 1 : -1)
                });
            })
            .catch(this.reportError);

        this.WebApiClient.Retrieve({entityName: "plugintype", queryParams: "?$filter=assemblyname eq 'Xrm.Oss.XTL.Templating'&$expand=plugintype_sdkmessageprocessingstep"})
        .then((result: any) => this.WebApiClient.Expand({records: result.value}))
        .then((result: any) => {
            if (!result.length) {
                return;
            }

            const pluginType = result[0];

            this.setState({pluginType: pluginType});
        })
        .catch(this.reportError);
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
        })
        .catch(this.reportError);
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

    showSdkSteps() {
        this.setState({
            showSdkSteps: true
        });
    }

    setSelectedSdkStep(eventKey: any) {
        const selectedSdkStep = this.state.pluginType.plugintype_sdkmessageprocessingstep.value.find((step: any) => step.sdkmessageprocessingstepid === eventKey);
        const config = JSON.parse(selectedSdkStep.configuration) || {};

        this.setState({
            executionCriteria: config.executionCriteria || "",
            inputTemplate: config.template || "",
            showSdkSteps: false,
            selectedSdkStep: selectedSdkStep
        });
    }

    saveSelectedSdkStep() {
        this.setState({requestPending: true});

        // TODO: Set/Implement FilteringAttributes

        if (this.state.selectedSdkStep.sdkmessageprocessingstepid) {
            const config = JSON.parse(this.state.selectedSdkStep.configuration) || {};

            config.executionCriteria = this.state.executionCriteria;
            config.template = this.state.inputTemplate;

            this.WebApiClient.Update({
                entityName: "sdkmessageprocessingstep",
                entityId: this.state.selectedSdkStep.sdkmessageprocessingstepid,
                entity: {
                    configuration: JSON.stringify(config)
                }
            })
            .then((result: any) => {
                this.setState({requestPending: false});
            })
            .catch(this.reportError);
        }
        else {
            const config = {
                executionCriteria: this.state.executionCriteria,
                template: this.state.inputTemplate
            };

            const messageName = (this.state.selectedSdkStep as any).messageName;
            delete (this.state.selectedSdkStep as any).messageName;

            this.WebApiClient.Create({
                entityName: "sdkmessageprocessingstep",
                entity: {
                    ...this.state.selectedSdkStep,
                    configuration: JSON.stringify(config)
                }
            })
            .then((result: any) => {
                this.setState({requestPending: false});

                // Return in format of https://host/api/data/v8.0/sdkmessageprocessingstep(e74471fd-fa40-e811-a836-000d3ab4d04c)
                const stepId = result.substr(result.indexOf("(") + 1, 36);
                if (messageName !== "Create") {
                    const image = {
                        entityalias: "preimg",
                        name: "preimg",
                        imagetype: 0,
                        messagepropertyname: "Target"
                    } as any;

                    image["sdkmessageprocessingstepid@odata.bind"] = `/sdkmessageprocessingsteps(${stepId})`;

                    return this.WebApiClient.Create({
                        entityName: "sdkmessageprocessingstepimage",
                        entity: image
                    });
                }
            })
            .catch(this.reportError);
        }
    }

    createNewSdkStep () {
        this.setState({
            showSdkStepCreationDialog: true
        });
    }

    sdkStepCreationHandler (step: SdkStep) {
        const update = {
            showSdkStepCreationDialog: false
        } as WYSIWYGEditorState;

        if (step) {
            update.selectedSdkStep = step;
        }

        this.setState(update);
    }

    reportError (e: any) {
        this.setState({
            success: false,
            requestPending: false,
            error: e.message ? e.message : e
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

            <Modal.Body>Please Wait...</Modal.Body>
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
          {this.state.showSdkSteps &&
            <Modal.Dialog>
            <Modal.Header>
              <Modal.Title>Select SDK Step</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <DropdownButton
                    bsStyle="default"
                    title={this.state.selectedSdkStep ? this.state.selectedSdkStep.name : "Select SDK Step" }
                    id="SdkStepSelect"
                >
                      { this.state.pluginType.plugintype_sdkmessageprocessingstep.value.map( (value: any) => <MenuItem onSelect={this.setSelectedSdkStep} eventKey={value.sdkmessageprocessingstepid}>{value.name}</MenuItem> ) }
                </DropdownButton>
            </Modal.Body>
          </Modal.Dialog>}
          <StepCreationDialog isVisible={this.state.showSdkStepCreationDialog} entities={this.state.entities} stepCallBack={this.sdkStepCreationHandler} errorCallBack={this.reportError} pluginTypeId={this.state.pluginType ? this.state.pluginType.plugintypeid : ""} />
          {this.state.selectedSdkStep && <a>SDK Step: {this.state.selectedSdkStep.name}</a>}
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
                <Button bsStyle="default" onClick={this.showSdkSteps}>Load From SDK Steps</Button>
                <Button bsStyle="default" disabled={!this.state.selectedSdkStep} onClick={this.saveSelectedSdkStep}>Update Selected SDK Step</Button>
                <Button bsStyle="default" onClick={this.createNewSdkStep}>Create New SDK Step</Button>
              </ButtonGroup>
            </ButtonToolbar>
              <FormGroup className="col-xs-6" controlId="input">
                <ControlLabel>Execution Criteria</ControlLabel>
                <FormControl style={ { "height": "25vh", "overflow": "auto" } } onChange={ this.criteriaChanged } value={this.state.executionCriteria} componentClass="textarea" placeholder="Leave empty for executing unconditionally" />
                <ControlLabel style={{"padding-top": "10px"}}>Template</ControlLabel>
                <FormControl style={ { "height": "75vh", "overflow": "auto" } } onChange={ this.inputChanged } value={this.state.inputTemplate} componentClass="textarea" placeholder="Enter template" />
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

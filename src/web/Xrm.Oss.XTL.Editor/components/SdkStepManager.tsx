import * as React from "react";
import * as WebApiClient from "xrm-webapi-client";
import { EntityDefinition } from "../domain/EntityDefinition";
import { SdkStep } from "../domain/SdkStep";
import { SdkFilter } from "../domain/SdkFilter";
import { Well, ButtonToolbar, ButtonGroup, Button, DropdownButton, MenuItem, Modal, FormGroup, ControlLabel, FormControl } from "react-bootstrap";

export interface SdkStepManagerProps {
    entities: Array<EntityDefinition>;
    stepCallBack: (step: SdkStep, stepEntityLogicalName: string, stepMessageName: string) => void;
    errorCallBack: (e: any) => void;
    isVisible: boolean;
    pluginTypeId: string;
    pluginType: any;
}

interface SdkStepManagerState {
    selectedEntity?: string;
    selectedFilter?: SdkFilter;
    selectedSdkStep?: SdkStep;
    filters?: Array<SdkFilter>;
}

export class SdkStepManager extends React.PureComponent<SdkStepManagerProps, SdkStepManagerState> {
    constructor(props: SdkStepManagerProps) {
        super(props);

        this.state = {
        };

        this.setEntity = this.setEntity.bind(this);
        this.setMessage = this.setMessage.bind(this);
        this.newStep = this.newStep.bind(this);
        this.cancel = this.cancel.bind(this);
        this.setSelectedSdkStep = this.setSelectedSdkStep.bind(this);
        this.fireCallBack = this.fireCallBack.bind(this);
    }

    setEntity(eventKey: any) {
        this.setState({
            selectedEntity: eventKey
        });

        WebApiClient.Retrieve({
            entityName: "sdkmessagefilter",
            queryParams: `?$filter=primaryobjecttypecode eq '${eventKey}'&$expand=sdkmessageid`})
        .then((result: any) => {
            this.setState({
                filters: result.value
            });
        })
        .catch(this.props.errorCallBack);
    }

    setMessage(eventKey: any) {
        this.setState({
            selectedFilter: this.state.filters.find(filter => filter.sdkmessagefilterid === eventKey)
        });
    }

    newStep() {
        const step: any = {
            name: `Xrm.Oss.XTL.Templating.XTLProcessor: ${this.state.selectedFilter.sdkmessageid.name} of ${this.state.selectedEntity}`,
            mode: 0,
            rank: 1,
            stage: 20
        };

        step["sdkmessagefilterid@odata.bind"] = `/sdkmessagefilters(${this.state.selectedFilter.sdkmessagefilterid})`;
        step["sdkmessageid@odata.bind"] = `/sdkmessages(${this.state.selectedFilter.sdkmessageid.sdkmessageid})`;
        step["plugintypeid@odata.bind"] = `/plugintypes(${this.props.pluginTypeId})`;
        step["messageName"] = this.state.selectedFilter.sdkmessageid.name;

        return step;
    }

    setSelectedSdkStep(eventKey: any) {
        if (!eventKey) {
            return this.setState({
                selectedSdkStep: {
                    name: "Create New"
                }
            });
        }

        const step = this.props.pluginType.plugintype_sdkmessageprocessingstep.value.find((step: any) => step.sdkmessageprocessingstepid === eventKey);

        this.setState({
            selectedSdkStep: step
        });
    }

    fireCallBack () {
        if (!this.state.selectedSdkStep.sdkmessageprocessingstepid) {
            return this.props.stepCallBack(this.newStep(), this.state.selectedFilter.primaryobjecttypecode, this.state.selectedFilter.sdkmessageid.name);
        }

        return WebApiClient.Retrieve({
            entityName: "sdkmessagefilter",
            entityId: this.state.selectedSdkStep._sdkmessagefilterid_value,
            queryParams: `?$expand=sdkmessageid`})
        .then((result: any) => {
            const filter = result as SdkFilter;
            return this.props.stepCallBack(this.state.selectedSdkStep, filter.primaryobjecttypecode, filter.sdkmessageid.name);
        })
        .catch(this.props.errorCallBack);
    }

    cancel() {
        this.props.stepCallBack(undefined, undefined, undefined);
    }

    render() {
        return <div>
            {this.props.isVisible &&
              <Modal.Dialog>
              <Modal.Header>
                <Modal.Title>Manage SDK Steps</Modal.Title>
              </Modal.Header>
              <Modal.Body>
                  <ButtonToolbar style={{"padding-bottom": "10px"}}>
                    <ButtonGroup>
                        <DropdownButton
                            bsStyle="default"
                            title={this.state.selectedSdkStep ? this.state.selectedSdkStep.name : "Select SDK Step" }
                            id="SdkStepSelect"
                        >
                              { [{sdkmessageprocessingstepid: undefined, name: "Create New"}].concat(this.props.pluginType.plugintype_sdkmessageprocessingstep.value).map( (value: any) => <MenuItem onSelect={this.setSelectedSdkStep} eventKey={value.sdkmessageprocessingstepid}>{value.name}</MenuItem> ) }
                        </DropdownButton>
                  {this.state.selectedSdkStep && !this.state.selectedSdkStep.sdkmessageprocessingstepid && <DropdownButton
                      bsStyle="default"
                      title={this.state.selectedEntity ? this.props.entities.find(e => e.LogicalName == this.state.selectedEntity).SchemaName : "Entity" }
                      id="EntitySelect"
                  >
                        { this.props.entities.map( value => <MenuItem onSelect={this.setEntity} eventKey={value.LogicalName}>{value.SchemaName}</MenuItem> ) }
                  </DropdownButton>
                  }
                  {this.state.selectedSdkStep && !this.state.selectedSdkStep.sdkmessageprocessingstepid && <DropdownButton
                      bsStyle="default"
                      disabled={!this.state.selectedEntity || !this.state.filters}
                      title={this.state.selectedFilter ? this.state.selectedFilter.sdkmessageid.name : "Select Message" }
                      id="FilterSelect"
                  >
                        { this.state.filters && this.state.filters.map( value => <MenuItem onSelect={this.setMessage} eventKey={value.sdkmessagefilterid}>{value.sdkmessageid.name}</MenuItem> ) }
                  </DropdownButton>
                  }
                  </ButtonGroup>
                </ButtonToolbar>
              </Modal.Body>
              <Modal.Footer>
                  <Button bsStyle="default" disabled={!this.state.selectedSdkStep && (!this.state.selectedEntity || !this.state.selectedFilter)} onClick={ this.fireCallBack }>Ok</Button>
                  <Button bsStyle="default" onClick={ this.cancel }>Cancel</Button>
              </Modal.Footer>
            </Modal.Dialog>}
        </div>;
    }
}

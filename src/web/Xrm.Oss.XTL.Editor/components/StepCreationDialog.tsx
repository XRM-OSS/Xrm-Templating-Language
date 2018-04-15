import * as React from "react";
import WebApiClient from "xrm-webapi-client";
import { EntityDefinition } from "../domain/EntityDefinition";
import { SdkStep } from "../domain/SdkStep";
import { SdkFilter } from "../domain/SdkFilter";
import { Well, ButtonToolbar, ButtonGroup, Button, DropdownButton, MenuItem, Modal, FormGroup, ControlLabel, FormControl } from "react-bootstrap";

export interface StepCreationDialogProps {
    entities: Array<EntityDefinition>;
    stepCallBack: (step: SdkStep) => void;
    errorCallBack: (e: any) => void;
    isVisible: boolean;
    pluginTypeId: string;
}

interface StepCreationDialogState {
    selectedEntity?: string;
    selectedFilter?: SdkFilter;
    filters?: Array<SdkFilter>;
}

export class StepCreationDialog extends React.PureComponent<StepCreationDialogProps, StepCreationDialogState> {
    private WebApiClient: typeof WebApiClient;

    constructor(props: StepCreationDialogProps) {
        super(props);

        this.state = {
        };

        // Webpack should import WebApiClient from global itself, but somehow it doesn't
        this.WebApiClient = (window as any).WebApiClient;
        this.setEntity = this.setEntity.bind(this);
        this.setMessage = this.setMessage.bind(this);
        this.setStep = this.setStep.bind(this);
        this.cancel = this.cancel.bind(this);
    }

    setEntity(eventKey: any) {
        this.setState({
            selectedEntity: eventKey
        });

        this.WebApiClient.Retrieve({
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

    setStep() {
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

        this.props.stepCallBack(step);
    }

    cancel() {
        this.props.stepCallBack(undefined);
    }

    render() {
        return <div>
            {this.props.isVisible &&
              <Modal.Dialog>
              <Modal.Header>
                <Modal.Title>Create new SDK Step</Modal.Title>
              </Modal.Header>
              <Modal.Body>
                  <ButtonToolbar style={{"padding-bottom": "10px"}}>
                    <ButtonGroup>
                  <DropdownButton
                      bsStyle="default"
                      title={this.state.selectedEntity ? this.props.entities.find(e => e.LogicalName == this.state.selectedEntity).SchemaName : "Entity" }
                      id="EntitySelect"
                  >
                        { this.props.entities.map( value => <MenuItem onSelect={this.setEntity} eventKey={value.LogicalName}>{value.SchemaName}</MenuItem> ) }
                  </DropdownButton>
                  <DropdownButton
                      bsStyle="default"
                      disabled={!this.state.selectedEntity || !this.state.filters}
                      title={this.state.selectedFilter ? this.state.selectedFilter.sdkmessageid.name : "Select Message" }
                      id="FilterSelect"
                  >
                        { this.state.filters && this.state.filters.map( value => <MenuItem onSelect={this.setMessage} eventKey={value.sdkmessagefilterid}>{value.sdkmessageid.name}</MenuItem> ) }
                  </DropdownButton>
                  </ButtonGroup>
                </ButtonToolbar>
              </Modal.Body>
              <Modal.Footer>
                  <Button bsStyle="default" disabled={!this.state.selectedEntity || !this.state.selectedFilter} onClick={ this.setStep }>Ok</Button>
                  <Button bsStyle="default" onClick={ this.cancel }>Cancel</Button>
              </Modal.Footer>
            </Modal.Dialog>}
        </div>;
    }
}

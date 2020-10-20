import * as React from "react";
import { EntityDefinition } from "../domain/EntityDefinition";
import { Attribute } from "../domain/Attribute";
import * as WebApiClient from "xrm-webapi-client";
import { SdkStepManager } from "./SdkStepManager";
import { SdkStep } from "../domain/SdkStep";
import { Well, ButtonToolbar, ButtonGroup, Button, DropdownButton, MenuItem, Panel, InputGroup, Modal, FormGroup, ControlLabel, FormControl, ListGroup, ListGroupItem, Checkbox } from "react-bootstrap";
import parse from "html-react-parser";
import MonacoEditor from "react-monaco-editor";
import * as monacoEditor from "monaco-editor";

interface XtlEditorState {
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
  selectedSdkStep: SdkStep;
  showSdkStepManager: boolean;
  templateField: string;
  targetField: string;
  sdkStepName: string;
  rank: number;
  sdkStepEntityLogicalName?: string;
  sdkStepMessageName?: string;
  sdkEntityAttributes?: Array<Attribute>;
  selectedEntityAttributes: Array<string>;
  isHtmlTemplate?: boolean;
}

export default class XtlEditor extends React.PureComponent<any, XtlEditorState> {
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
          selectedSdkStep: undefined,
          showSdkStepManager: false,
          templateField: "",
          targetField: "",
          sdkStepName: "",
          rank: 1,
          selectedEntityAttributes: []
        };

        this.criteriaChanged = this.criteriaChanged.bind(this);
        this.preview = this.preview.bind(this);
        this.selectTarget = this.selectTarget.bind(this);
        this.setTypeCode = this.setTypeCode.bind(this);
        this.copy = this.copy.bind(this);
        this.closeCopyDialog = this.closeCopyDialog.bind(this);
        this.setSdkStep = this.setSdkStep.bind(this);
        this.saveSelectedSdkStep = this.saveSelectedSdkStep.bind(this);
        this.reportError = this.reportError.bind(this);
        this.clearError = this.clearError.bind(this);
        this.openSdkStepManager = this.openSdkStepManager.bind(this);
        this.targetFieldChanged = this.targetFieldChanged.bind(this);
        this.templateFieldChanged = this.templateFieldChanged.bind(this);
        this.activateSelectedSdkStep = this.activateSelectedSdkStep.bind(this);
        this.deactivateSelectedSdkStep = this.deactivateSelectedSdkStep.bind(this);
        this.deleteSelectedSdkStep = this.deleteSelectedSdkStep.bind(this);
        this.stepNameChanged = this.stepNameChanged.bind(this);
        this.onSelectEntityAttribute = this.onSelectEntityAttribute.bind(this);
        this.isHtmlTemplateChanged = this.isHtmlTemplateChanged.bind(this);
        this.rankChanged = this.rankChanged.bind(this);

        window.onresize = () => {
            (this.refs.criteriaEditor as any).editor.layout();
            (this.refs.templateEditor as any).editor.layout();
        };
    }

    componentDidMount() {
        this.setState({
            requestPending: true
        });

        WebApiClient.Promise.all([
            this.retrieveEntities(),
            this.retrievePluginType()
        ])
        .then(_ => {
            this.setState({
                requestPending: false
            });
        });
    }

    retrieveEntities (): Promise<any> {
        return WebApiClient.Retrieve({entityName: "EntityDefinition", queryParams: "?$select=ObjectTypeCode,SchemaName,LogicalName&$filter=IsValidForAdvancedFind eq true"})
            .then((result: any) => {
                this.setState({
                    entities: (result.value as Array<any>).filter(e => e.SchemaName).sort((e1, e2) => e1.SchemaName >= e2.SchemaName ? 1 : -1)
                });
            })
            .catch(this.reportError);
    }

    retrievePluginType (): Promise<any> {
        return WebApiClient.Retrieve({entityName: "plugintype", queryParams: "?$filter=assemblyname eq 'Xrm.Oss.XTL.Templating'&$expand=plugintype_sdkmessageprocessingstep", })
        .then((result: any) => (WebApiClient as any).Expand({records: result.value}))
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

        const request = WebApiClient.Requests.Request.prototype.with({
            method: "POST",
            name: "oss_XTLApplyTemplate",
            bound: false
        });

        let template = this.state.inputTemplate;

        // This needs to be done before interpreting if it"s an HTML template
        // RecordTables will mess up otherwise, as they create linebreaks inside
        if (this.state.isHtmlTemplate) {
            template = template.replace(/\n/g, "<br />");
        }

        WebApiClient.Execute(request.with({
            payload: {
                jsonInput: JSON.stringify({
                    target: {
                        Id: this.state.selectedEntityId,
                        LogicalName: this.state.selectedEntityLogicalName
                    },
                    organizationUrl: WebApiClient.GetApiUrl().substring(0, WebApiClient.GetApiUrl().indexOf("/api/data/v")),
                    template: template,
                    templateField: this.state.templateField,
                    executionCriteria: this.state.executionCriteria
                })
            }
        }))
        .then((result: any) => {
            const json = JSON.parse(result.jsonOutput);
            let resultText = (json.result || "");

            if (!this.state.isHtmlTemplate) {
                resultText = resultText.replace(/\n/g, "<br />");
            }

            this.setState({
                requestPending: false,
                resultText: resultText,
                traceLog: json.traceLog
            });
        })
        .catch(this.reportError);
    }

    selectTarget(e: any) {
        const url = WebApiClient.GetApiUrl().replace("/api/data/v8.0/", "") + `/_controls/lookup/lookupinfo.aspx?AllowFilterOff=1&DisableQuickFind=0&DisableViewPicker=0&LookupStyle=single&ShowNewButton=0&ShowPropButton=0&browse=false&objecttypes=${this.state.selectedTypeCode}`;
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

    isHtmlTemplateChanged(e: any) {
      this.setState({
        isHtmlTemplate: e.target.checked
      });
    }

    criteriaChanged(newValue: any, e: any) {
      this.setState({
        executionCriteria: newValue
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

    setSdkStep(step: SdkStep, sdkStepEntityLogicalName: string, sdkStepMessageName: string) {
        let config: any = {};

        if (!step) {
            return this.setState({
                showSdkStepManager: false
            });
        }

        if (step.configuration) {
            config = JSON.parse(step.configuration) || {};
        }

        let template = config.template || "";

        if (config.isHtmlTemplate) {
            template = template.replace(/<br \/>/g, "\n");
        }

        this.setState({
            executionCriteria: config.executionCriteria || "",
            inputTemplate: template,
            templateField: config.templateField || "",
            targetField: config.targetField || "",
            isHtmlTemplate: config.isHtmlTemplate,
            showSdkStepManager: false,
            selectedSdkStep: step,
            sdkStepName: step.name,
            rank: step.rank,
            sdkStepEntityLogicalName: sdkStepEntityLogicalName,
            sdkStepMessageName: sdkStepMessageName,
            selectedEntityAttributes: step.filteringattributes ? step.filteringattributes.split(",") : [],
            selectedTypeCode: this.state.entities.find(e => e.LogicalName === sdkStepEntityLogicalName).ObjectTypeCode
        });

        const entity = this.state.entities.find(e => e.LogicalName === sdkStepEntityLogicalName);

        if (entity) {
            const request = {
               entityName: "EntityDefinition",
               entityId: entity.MetadataId,
               queryParams: "/Attributes?$select=LogicalName,DisplayName"
            };

            return WebApiClient.Retrieve(request)
               .then((response: any) => {
                   const attributes = (response.value as Array<Attribute>);

                   attributes.sort(function(e1, e2) {
                        if (e1.LogicalName < e2.LogicalName) {
                            return -1;
                        }

                        if (e1.LogicalName > e2.LogicalName) {
                            return 1;
                        }

                        return 0;
                    });

                   this.setState({
                       sdkEntityAttributes: attributes
                   });
               });
        }

        return undefined;
    }

    ensureSdkStepSecureConfig = (step: SdkStep): Promise<string> => {
        if (this.state.selectedSdkStep._sdkmessageprocessingstepsecureconfigid_value) {
            return Promise.resolve(this.state.selectedSdkStep._sdkmessageprocessingstepsecureconfigid_value);
        }

        return WebApiClient.Create({
            entityName: "sdkmessageprocessingstepsecureconfig",
            entity: {
                secureconfig: JSON.stringify({
                    organizationUrl: WebApiClient.GetApiUrl().substring(0, WebApiClient.GetApiUrl().indexOf("/api/data/v"))
                })
            }
        })
        .then((result: string) => {
            const configId = result.substr(result.indexOf("(") + 1, 36);

            (step as any)["sdkmessageprocessingstepsecureconfigid@odata.bind"] = `/sdkmessageprocessingstepsecureconfigs(${configId})`;
            return configId;
        });
    }

    saveSelectedSdkStep() {
        this.setState({requestPending: true});

        const config = this.state.selectedSdkStep.configuration ? JSON.parse(this.state.selectedSdkStep.configuration) || {} : {};

        let template = this.state.inputTemplate;

        if (this.state.isHtmlTemplate) {
            template = template.replace(/\n/g, "<br />");
        }

        config.executionCriteria = this.state.executionCriteria;
        config.template = template;
        config.templateField = this.state.templateField;
        config.targetField = this.state.targetField;
        config.isHtmlTemplate = this.state.isHtmlTemplate;

        const step = {
            name: this.state.sdkStepName,
            rank: this.state.rank,
            configuration: JSON.stringify(config),
            filteringattributes: this.state.selectedEntityAttributes.join(","),
        };

        this.ensureSdkStepSecureConfig(step)
        .then(configId => {
            if (this.state.selectedSdkStep.sdkmessageprocessingstepid) {
                WebApiClient.Update({
                    entityName: "sdkmessageprocessingstep",
                    entityId: this.state.selectedSdkStep.sdkmessageprocessingstepid,
                    entity: step
                })
                .then((result: any) => {
                    this.setState({
                        requestPending: false,
                        selectedSdkStep: {
                            ...this.state.selectedSdkStep,
                            _sdkmessageprocessingstepsecureconfigid_value: configId
                        }
                    });
                })
                .catch(this.reportError);
            }
            else {
                const messageName = (this.state.selectedSdkStep as any).messageName;
                delete (this.state.selectedSdkStep as any).messageName;

                WebApiClient.Create({
                    entityName: "sdkmessageprocessingstep",
                    entity: {
                        ...this.state.selectedSdkStep,
                        ...step
                    }
                })
                .then((result: any) => {
                    // Return in format of https://host/api/data/v8.0/sdkmessageprocessingstep(e74471fd-fa40-e811-a836-000d3ab4d04c)
                    const stepId = result.substr(result.indexOf("(") + 1, 36);

                    this.setState({
                        requestPending: false,
                        selectedSdkStep: {
                            ...this.state.selectedSdkStep,
                            sdkmessageprocessingstepid: stepId,
                            _sdkmessageprocessingstepsecureconfigid_value: configId
                        }
                    });

                    if (messageName !== "Create") {
                        const image = {
                            entityalias: "preimg",
                            name: "preimg",
                            imagetype: 0,
                            messagepropertyname: "Target"
                        } as any;

                        image["sdkmessageprocessingstepid@odata.bind"] = `/sdkmessageprocessingsteps(${stepId})`;

                        return WebApiClient.Create({
                            entityName: "sdkmessageprocessingstepimage",
                            entity: image
                        });
                    }
                })
                .catch(this.reportError);
            }
        });
    }

    saveAs() {
        this.setState({
            selectedSdkStep: {
                ...this.state.selectedSdkStep,
                name: `Copy of ${this.state.sdkStepName}`,
                sdkmessageprocessingstepid: undefined,
                _sdkmessageprocessingstepsecureconfigid_value: undefined
            }
        }, this.saveSelectedSdkStep);
    }

    activateSelectedSdkStep () {
        this.setState({requestPending: true});

        WebApiClient.Update({
            entityName: "sdkmessageprocessingstep",
            entityId: this.state.selectedSdkStep.sdkmessageprocessingstepid,
            entity: {
                statecode: 0
            }
        })
        .then((result: any) => {
            this.setState({
                requestPending: false
            });
        })
        .catch(this.reportError);
    }

    deactivateSelectedSdkStep () {
        this.setState({requestPending: true});

        WebApiClient.Update({
            entityName: "sdkmessageprocessingstep",
            entityId: this.state.selectedSdkStep.sdkmessageprocessingstepid,
            entity: {
                statecode: 1
            }
        })
        .then((result: any) => {
            this.setState({
                requestPending: false
            });
        })
        .catch(this.reportError);
    }

    deleteSelectedSdkStep () {
        this.setState({requestPending: true});

        (WebApiClient.Delete({
            entityName: "sdkmessageprocessingstep",
            entityId: this.state.selectedSdkStep.sdkmessageprocessingstepid
        }) as Promise<string>)
        .then((result: any) => {
            this.setState({
                selectedSdkStep: undefined,
                requestPending: false
            });
        })
        .catch(this.reportError);
    }

    reportError (e: any) {
        this.setState({
            success: false,
            requestPending: false,
            error: e.message ? e.message : e
        });
    }

    clearError() {
        this.setState({
            success: true,
            error: undefined
        });
    }

    openSdkStepManager() {
        this.retrievePluginType()
        .then(_ => {
            this.setState({
                showSdkStepManager: true
            });
        });
    }

    templateFieldChanged(e: any) {
      this.setState({
        templateField: e.target.value
      });
    }

    targetFieldChanged(e: any) {
      this.setState({
        targetField: e.target.value
      });
    }

    stepNameChanged(e: any) {
      this.setState({
        sdkStepName: e.target.value
      });
    }

    rankChanged(e: any) {
      this.setState({
        rank: e.target.value
      });
    }

    onSelectEntityAttribute (e: any) {
        const attributeId = e.currentTarget.id;
        const attributeIndex = this.state.selectedEntityAttributes.indexOf(attributeId);

        if (this.state.sdkStepMessageName !== "Update") {
            return;
        }

        if (attributeIndex !== -1) {
            this.setState({
                selectedEntityAttributes: this.state.selectedEntityAttributes.filter((attr, index) => index !== attributeIndex)
            });
        }
        else {
            this.setState({
                selectedEntityAttributes: this.state.selectedEntityAttributes.concat([attributeId])
            });
        }
    }

    onChange = (newValue: any, e: any) => {
        this.setState({
          inputTemplate: newValue
        });
    }

    registerXtl = (monaco: any, isTemplateEditor: boolean) => {
        const root = [
          [/\${{/, { token: "keyword", switchTo: "@xtl" }],
        ];

        const xtl = [
          // This must stay first, it will be removed when not being in template but in criteria editor
          [/}}/, {token: "keyword", switchTo: "@root"}],

          // identifiers and keywords
          [/[a-z_$][\w$]*/, { cases: { "@typeKeywords": "keyword",
                                       "@keywords": "keyword",
                                       "@default": "identifier" } }],
          [/[A-Z][\w\$]*/, "type.identifier" ],  // to show class names nicely

          // whitespace
          { include: "@whitespace" },

          // delimiters and operators
          [/[{}()\[\]]/, "@brackets"],
          [/@symbols/, { cases: { "@operators": "operator",
                                  "@default"  : "" } } ],

          // @ annotations.
          // As an example, we emit a debugging log message on these tokens.
          // Note: message are supressed during the first load -- change some lines to see them.
          [/@\s*[a-zA-Z_\$][\w\$]*/, { token: "annotation", log: "annotation token: $0" }],

          // numbers
          [/\d*\.\d+([eE][\-+]?\d+)?/, "number.float"],
          [/0[xX][0-9a-fA-F]+/, "number.hex"],
          [/\d+/, "number"],

          // delimiter: after number because of .\d floats
          [/[;,.]/, "delimiter"],

          // strings
          [/"([^"\\]|\\.)*$/, "string.invalid"],  // non-teminated string
          [/'([^'\\]|\\.)*$/, "string.invalid"],  // non-teminated string
          [/"/, "string", "@string_double"],
          [/'/, "string", "@string_single"]
        ];

        const id = isTemplateEditor ? "XTL_Template" : "XTL";
        // Register a new language
        monaco.languages.register({ id: id });

        monaco.languages.setLanguageConfiguration(id, {
            brackets: [
                ["{", "}"],
                ["[", "]"],
                ["(", ")"],
            ],

            autoClosingPairs: [
                { open: "{", close: "}" },
                { open: "[", close: "]" },
                { open: "(", close: ")" },
                { open: "\"", close: "\"" },
                { open: "'", close: "'" },
            ],

            surroundingPairs: [
                { open: "{", close: "}" },
                { open: "[", close: "]" },
                { open: "(", close: ")" },
                { open: "\"", close: "\"" },
                { open: "'", close: "'" }
            ]
        });

        // Register a tokens provider for the language
        monaco.languages.setMonarchTokensProvider(id, {
            keywords: [
                "null", "true", "false"
              ],

            typeKeywords: [

            ],

            operators: [ ],

            // we include these common regular expressions
            symbols:  /[=><!~?:&|+\-*\/\^%]+/,

            // C# style strings
            escapes: /\\(?:[abfnrtv\\""]|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

            tokenizer: {
                // If template editor with place holders is used, only highlight placeholders
                // Execution Criteria is always an XTL expression, so it does not need the rule for switching to the unhighlighted root
                root: isTemplateEditor ? root : xtl.slice(1),

                xtl: xtl,

                comment: [
                  [/[^\/*]+/, "comment" ],
                  [/\/\*/,    "comment", "@push" ],    // nested comment
                  ["\\*/",    "comment", "@pop"  ],
                  [/[\/*]/,   "comment" ]
                ],

                string_double: [
                    [/[^\\"]+/, "string"],
                    [/@escapes/, "string.escape"],
                    [/\\./, "string.escape.invalid"],
                    [/"/, "string", "@pop"]
                ],

                string_single: [
                    [/[^\\']+/, "string"],
                    [/@escapes/, "string.escape"],
                    [/\\./, "string.escape.invalid"],
                    [/'/, "string", "@pop"]
                ],

                whitespace: [
                  [/[ \t\r\n]+/, "white"],
                  [/\/\*/,       "comment", "@comment" ],
                  [/\/\/.*$/,    "comment"],
                ],
              }
            });

        // Register a completion item provider for the new language
        monaco.languages.registerCompletionItemProvider(id, {
            provideCompletionItems: () => {
                return [
                    {
                        label: "Value",
                        kind: monaco.languages.CompletionItemKind.Snippet,
                        insertText: {
                            value: "Value (\"\")"
                        }
                    }
                ];
            }
        });
    }

    templateEditorWillMount = (monaco: any) => {
        this.registerXtl(monaco, true);
    }

    criteriaEditorWillMount = (monaco: any) => {
        this.registerXtl(monaco, false);
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

            <Modal.Body>{JSON.stringify({template: this.state.isHtmlTemplate ? this.state.inputTemplate.replace(/\n/g, "<br />") : this.state.inputTemplate, executionCriteria: this.state.executionCriteria})}</Modal.Body>
            <Modal.Footer>
                <Button bsStyle="default" onClick={ this.closeCopyDialog }>Close</Button>
            </Modal.Footer>
          </Modal.Dialog>}
          <SdkStepManager isVisible={this.state.showSdkStepManager} pluginType={this.state.pluginType} entities={this.state.entities} stepCallBack={this.setSdkStep} errorCallBack={this.reportError} pluginTypeId={this.state.pluginType ? this.state.pluginType.plugintypeid : ""} />
          {this.state.selectedSdkStep && <a>SDK Step: {this.state.selectedSdkStep.name}</a>}
          {this.state.selectedSdkStep && this.state.selectedEntityId && <br />}
          {this.state.selectedEntityId && <a>Entity: {this.state.selectedEntityLogicalName}, Id: {this.state.selectedEntityId}, Name: {this.state.selectedEntityName}</a>}
          {!this.state.success &&
                <Modal.Dialog>
                <Modal.Header>
                  <Modal.Title>An Error occured</Modal.Title>
                </Modal.Header>

                <Modal.Body>Message: {this.state.error}</Modal.Body>
                <Modal.Footer>
                    <Button bsStyle="default" onClick={ this.clearError }>Close</Button>
                </Modal.Footer>
              </Modal.Dialog>}
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
                <Button bsStyle="default" disabled={!this.state.pluginType} onClick={this.openSdkStepManager}>Manage SDK Steps</Button>
                <Button bsStyle="default" disabled={!this.state.selectedSdkStep} onClick={this.saveSelectedSdkStep}>Save</Button>
                <Button bsStyle="default" disabled={!this.state.selectedSdkStep} onClick={this.saveAs}>Save As</Button>
                <Button bsStyle="default" disabled={!this.state.selectedSdkStep || !this.state.selectedSdkStep.sdkmessageprocessingstepid} onClick={this.activateSelectedSdkStep}>Activate</Button>
                <Button bsStyle="default" disabled={!this.state.selectedSdkStep || !this.state.selectedSdkStep.sdkmessageprocessingstepid} onClick={this.deactivateSelectedSdkStep}>Deactivate</Button>
                <Button bsStyle="default" disabled={!this.state.selectedSdkStep || !this.state.selectedSdkStep.sdkmessageprocessingstepid} onClick={this.deleteSelectedSdkStep}>Delete</Button>
              </ButtonGroup>
            </ButtonToolbar>
              <Panel hidden={!this.state.selectedSdkStep} id="pluginConfiguration">
                  <h3>Plugin Configuration</h3>
                  <FormGroup className="col-xs-6" controlId="input">
                    <ControlLabel>Step Name</ControlLabel>
                    <FormControl onChange={ this.stepNameChanged } value={this.state.sdkStepName} componentClass="textarea" placeholder="Enter name of SDK step" />
                    <ControlLabel style={{"padding-top": "10px"}}>Rank</ControlLabel>
                    <FormControl onChange={ this.rankChanged } value={this.state.rank} componentClass="textarea" placeholder="Execution Pipeline Rank" />
                    <ControlLabel style={{"padding-top": "10px"}}>Target Field</ControlLabel>
                    <FormControl onChange={ this.targetFieldChanged } value={this.state.targetField} componentClass="textarea" placeholder="Enter name of target field for result" />
                    <ControlLabel style={{"padding-top": "10px"}}>Template Field</ControlLabel>
                    <FormControl onChange={ this.templateFieldChanged } value={this.state.templateField} componentClass="textarea" placeholder="Enter name of template field (per-record templating)" />
                  </FormGroup>
                  <ControlLabel>Execute plugin on update of (Only for Update Steps)</ControlLabel>
                  <div style={ { "height": "25vh", "overflow": "auto", "border": "1px solid lightgray" } } className="col-xs-6">
                    <ListGroup>
                      {
                          this.state.sdkEntityAttributes && this.state.sdkEntityAttributes.filter(attr => attr.DisplayName && attr.DisplayName.UserLocalizedLabel).map(attr => (<ListGroupItem id={attr.LogicalName} header={attr.LogicalName} onClick={this.onSelectEntityAttribute} disabled={this.state.sdkStepMessageName !== "Update"} active={this.state.selectedEntityAttributes.indexOf(attr.LogicalName) !== -1}>{attr.DisplayName.UserLocalizedLabel.Label}</ListGroupItem>))
                      }
                    </ListGroup>
                  </div>
              </Panel>
              <Panel id="templateConfiguration">
                  <h3>Template Configuration</h3>
                  <FormGroup className="col-xs-6" controlId="input">
                    <ControlLabel>Execution Criteria</ControlLabel>
                    <div style={ { "height": "25vh" } }>
                        <MonacoEditor
                                language="XTL"
                                theme="vs"
                                value={ this.state.executionCriteria || "// Leave empty for executing unconditionally" }
                                onChange={this.criteriaChanged}
                                editorWillMount={this.criteriaEditorWillMount}
                                ref="criteriaEditor"
                        />
                    </div>
                    <ControlLabel style={{"padding-top": "10px"}}>Template</ControlLabel>
                    <Checkbox checked={this.state.isHtmlTemplate} onChange={this.isHtmlTemplateChanged}>Is HTML template</Checkbox>
                    <div style={ { "height": "75vh" } }>
                        <MonacoEditor
                                language="XTL_Template"
                                theme="vs"
                                value={ this.state.inputTemplate || "// Enter your template..." }
                                onChange={this.onChange}
                                editorWillMount={this.templateEditorWillMount}
                                ref="templateEditor"
                        />
                    </div>
                  </FormGroup>
                  <div className="col-xs-6">
                    <ControlLabel>Result</ControlLabel>
                    <div style={ { "height": "50vh", "border": "1px solid lightgray", "overflow": "auto" } }>{parse(this.state.resultText)}</div>
                    <FormGroup controlId="output">
                      <ControlLabel style={{"padding-top": "10px"}}>Interpreter Trace</ControlLabel>
                      <FormControl style={ { "height": "50vh", "overflow": "auto" } } componentClass="textarea" value={ this.state.traceLog } disabled />
                    </FormGroup>
                  </div>
              </Panel>
          </div>
        </div>
        );
    }
}

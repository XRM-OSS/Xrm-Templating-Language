import { SdkMessage } from "./SdkMessage";

export interface SdkFilter {
    primaryobjecttypecode: string;
    sdkmessagefilterid: string;
    sdkmessageid: SdkMessage;
}

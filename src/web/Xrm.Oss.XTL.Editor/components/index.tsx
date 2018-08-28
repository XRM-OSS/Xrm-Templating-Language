import * as React from "react";
import * as ReactDOM from "react-dom";
import App from "./App";

(self as any).MonacoEnvironment = {
  getWorkerUrl: function (moduleId: any, label: any) {
    if (label === "json") {
      return "./json.worker.bundle.js";
    }
    if (label === "css") {
      return "./css.worker.bundle.js";
    }
    if (label === "html") {
      return "./html.worker.bundle.js";
    }
    if (label === "typescript" || label === "javascript") {
      return "./ts.worker.bundle.js";
    }
    return "./editor.worker.bundle.js";
  }
};

ReactDOM.render(
    <App />,
    document.getElementById("root")
);

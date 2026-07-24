/** Module purpose: Implements main for the provider-neutral DB-Notifier Dashboard without direct database access. */
import React from "react";
import ReactDOM from "react-dom/client";
import { App } from "./App";
import { LocalisationProvider } from "./LocalisationProvider";
import "./generated/design-tokens.css";
import "./styles.css";

const root = document.getElementById("root");
if (!root) {
  throw new Error("Dashboard root element was not found.");
}

/** Mounts one already selected composition inside the shared language boundary. */
function renderApplication(application: React.ReactElement) {
  ReactDOM.createRoot(root!).render(
    <React.StrictMode>
      <LocalisationProvider>
        {application}
      </LocalisationProvider>
    </React.StrictMode>,
  );
}

const observerSandboxBuild =
  import.meta.env.VITE_DB_NOTIFIER_OBSERVER_SANDBOX === "local-test";
if (observerSandboxBuild) {
  void Promise.all([
    import("./ObserverSandboxApp"),
    import("./observerSandbox"),
  ]).then(([{ ObserverSandboxApp }, { isObserverSandboxEnabled }]) => {
    if (!isObserverSandboxEnabled(import.meta.env, window.location)) {
      throw new Error("Observer sandbox origin was refused.");
    }
    renderApplication(<ObserverSandboxApp />);
  });
} else {
  renderApplication(<App />);
}

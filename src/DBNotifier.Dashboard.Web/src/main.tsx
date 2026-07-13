/** Module purpose: Implements main for the provider-neutral DB-Notifier Dashboard without direct database access. */
import React from "react";
import ReactDOM from "react-dom/client";
import { App } from "./App";
import "./generated/design-tokens.css";
import "./styles.css";

const root = document.getElementById("root");
if (!root) {
  throw new Error("Dashboard root element was not found.");
}

ReactDOM.createRoot(root).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);

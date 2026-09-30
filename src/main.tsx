import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { UltraMap } from "./UltraMap";
import "./style.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <UltraMap />
  </StrictMode>,
);

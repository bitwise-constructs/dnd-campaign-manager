import { Configuration, LogLevel } from "@azure/msal-browser";

// Replace these with your Entra app registration values
const TENANT_ID = import.meta.env.VITE_AZURE_TENANT_ID || "YOUR_TENANT_ID";
const CLIENT_ID = import.meta.env.VITE_AZURE_CLIENT_ID || "YOUR_SPA_CLIENT_ID";
const API_CLIENT_ID = import.meta.env.VITE_API_CLIENT_ID || "YOUR_API_CLIENT_ID";

export const msalConfig: Configuration = {
  auth: {
    clientId: CLIENT_ID,
    authority: `https://login.microsoftonline.com/${TENANT_ID}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message) => {
        if (level === LogLevel.Error) console.error(message);
      },
      logLevel: LogLevel.Error,
    },
  },
};

export const loginRequest = {
  scopes: [`api://${API_CLIENT_ID}/access_as_user`],
};

export const apiScopes = [`api://${API_CLIENT_ID}/access_as_user`];

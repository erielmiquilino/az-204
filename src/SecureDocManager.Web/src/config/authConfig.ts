import type { Configuration, PopupRequest } from "@azure/msal-browser";

// MSAL configuration
export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_AZURE_CLIENT_ID || "YOUR-SPA-CLIENT-ID",
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_AZURE_TENANT_ID || "YOUR-TENANT-ID"}`,
    redirectUri: import.meta.env.VITE_REDIRECT_URI || "http://localhost:3000",
    postLogoutRedirectUri: import.meta.env.VITE_REDIRECT_URI || "http://localhost:3000",
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) {
          return;
        }
        switch (level) {
          case 0: // LogLevel.Error
            console.error(message);
            return;
          case 1: // LogLevel.Info
            console.info(message);
            return;
          case 2: // LogLevel.Verbose
            console.debug(message);
            return;
          case 3: // LogLevel.Warning
            console.warn(message);
            return;
        }
      },
    },
  },
};

const apiScope = import.meta.env.VITE_API_SCOPE_URI;

if (!apiScope) {
  // Isso garante que o app não vai rodar sem a configuração correta
  throw new Error(
    "VITE_API_SCOPE_URI is not defined in your environment variables. Please check your .env file."
  );
}

// Add here scopes for id token to be used at MS Identity Platform endpoints.
export const loginRequest: PopupRequest = {
  scopes: [apiScope, "User.Read"],
  prompt: "consent",
};

// Add here the endpoints for MS Graph API services you would like to use.
export const graphConfig = {
  graphMeEndpoint: "https://graph.microsoft.com/v1.0/me",
  graphUsersEndpoint: "https://graph.microsoft.com/v1.0/users",
};

// API endpoints
export const apiConfig = {
  baseUrl: import.meta.env.VITE_API_BASE_URL || "https://localhost:7000/api",
  endpoints: {
    documents: "/documents",
    users: "/users",
    auth: "/auth",
  },
}; 
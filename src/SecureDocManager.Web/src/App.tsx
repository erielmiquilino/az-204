
import { MsalProvider } from "@azure/msal-react";
import { PublicClientApplication, EventType } from "@azure/msal-browser";
import type { EventMessage, AuthenticationResult } from "@azure/msal-browser";
import { BrowserRouter as Router } from "react-router-dom";
import { Toaster } from "react-hot-toast";
import { msalConfig } from "./config/authConfig";
import AuthWrapper from "./components/AuthWrapper";
import { AuthProvider } from "./contexts/AuthContext";
import MainLayout from "./components/layout/MainLayout";
import AppRoutes from "./routes/AppRoutes";

// Create MSAL instance
const msalInstance = new PublicClientApplication(msalConfig);

// Account selection logic
const accounts = msalInstance.getAllAccounts();
if (accounts.length > 0) {
  msalInstance.setActiveAccount(accounts[0]);
}

// Event callbacks
msalInstance.addEventCallback((event: EventMessage) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const payload = event.payload as AuthenticationResult;
    const account = payload.account;
    msalInstance.setActiveAccount(account);
  }
});

function App() {
  return (
    <MsalProvider instance={msalInstance}>
      <Router>
        <AuthWrapper>
          <AuthProvider>
            <MainLayout>
              <AppRoutes />
            </MainLayout>
            <Toaster
              position="top-right"
              toastOptions={{
                duration: 4000,
                style: {
                  background: "#363636",
                  color: "#fff",
                },
                success: {
                  duration: 3000,
                  iconTheme: {
                    primary: "#4caf50",
                    secondary: "#fff",
                  },
                },
                error: {
                  duration: 4000,
                  iconTheme: {
                    primary: "#f44336",
                    secondary: "#fff",
                  },
                },
              }}
            />
          </AuthProvider>
        </AuthWrapper>
      </Router>
    </MsalProvider>
  );
}

export default App;

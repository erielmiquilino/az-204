import React, { createContext, useContext, useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { graphConfig, loginRequest } from "../config/authConfig";
import type { User } from "../types";
import { UserRole } from "../types";
import { useApiCall } from "../hooks/useApiCall";

interface AuthContextType {
  user: User | null;
  loading: boolean;
  error: string | null;
  refreshUser: () => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

interface AuthProviderProps {
  children: React.ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const { instance, accounts, inProgress } = useMsal();
  const { callApi } = useApiCall();
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchUserProfile = async () => {
    if (accounts.length === 0) {
      setUser(null);
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      // Get user profile from Microsoft Graph
      const request = {
        ...loginRequest,
        account: accounts[0],
      };

      const tokenResponse = await instance.acquireTokenSilent(request);
      
      const graphResponse = await fetch(graphConfig.graphMeEndpoint, {
        headers: {
          Authorization: `Bearer ${tokenResponse.accessToken}`,
        },
      });

      if (!graphResponse.ok) {
        throw new Error("Failed to fetch user profile from Graph");
      }

      const graphData = await graphResponse.json();

      // Get additional user data from our API (including role)
      const apiResponse = await callApi<User>(`/users/profile`, {
        showError: false,
      });

      if (apiResponse.success && apiResponse.data) {
        setUser({
          ...apiResponse.data,
          displayName: graphData.displayName || apiResponse.data.displayName,
          email: graphData.mail || graphData.userPrincipalName || apiResponse.data.email,
          department: graphData.department || apiResponse.data.department,
          jobTitle: graphData.jobTitle || apiResponse.data.jobTitle,
        });
      } else {
        // If API call fails, create user from Graph data with default role
        setUser({
          id: graphData.id,
          displayName: graphData.displayName || "User",
          email: graphData.mail || graphData.userPrincipalName || "",
          department: graphData.department,
          jobTitle: graphData.jobTitle,
          role: UserRole.Employee, // Default role
        });
      }
    } catch (err) {
      console.error("Error fetching user profile:", err);
      setError(err instanceof Error ? err.message : "Failed to fetch user profile");
      
      // Set basic user info even if full profile fetch fails
      if (accounts.length > 0) {
        setUser({
          id: accounts[0].localAccountId,
          displayName: accounts[0].name || "User",
          email: accounts[0].username || "",
          role: UserRole.Employee,
        });
      }
    } finally {
      setLoading(false);
    }
  };

  const refreshUser = async () => {
    await fetchUserProfile();
  };

  const logout = () => {
    instance.logoutPopup().catch((error) => {
      console.error("Logout failed:", error);
      // Fallback to redirect
      instance.logoutRedirect();
    });
  };

  useEffect(() => {
    if (inProgress === InteractionStatus.None) {
      fetchUserProfile();
    }
  }, [accounts, inProgress]);

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        error,
        refreshUser,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}; 
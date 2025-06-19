import React, { useEffect, useState, useCallback, useRef } from "react";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { graphConfig } from "../config/authConfig";
import type { User } from "../types";
import { UserRole } from "../types";
import { useApiCall } from "../hooks/useApiCall";
import { AuthContext } from "./useAuth";

interface AuthProviderProps {
  children: React.ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const { instance, accounts, inProgress } = useMsal();
  const { callApi } = useApiCall();
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 3;
  const isRetryingRef = useRef(false);

  const fetchUserProfile = useCallback(async () => {
    if (accounts.length === 0) {
      setUser(null);
      setLoading(false);
      return;
    }

    // Previne múltiplas tentativas simultâneas
    if (isRetryingRef.current) {
      return;
    }

    // Se já tentou o máximo de vezes, para
    if (retryCount >= maxRetries) {
      console.error("Máximo de tentativas excedido. Parando de tentar buscar o perfil.");
      setError("Não foi possível carregar o perfil do usuário após múltiplas tentativas.");
      setLoading(false);
      return;
    }

    try {
      isRetryingRef.current = true;
      setLoading(true);
      setError(null);

      // Requisição específica para o Microsoft Graph com apenas o scope User.Read
      const graphRequest = {
        scopes: ["User.Read"],
        account: accounts[0],
      };

      console.log("Adquirindo token para Microsoft Graph...");
      const tokenResponse = await instance.acquireTokenSilent(graphRequest);
      
      console.log("Token acquired successfully");
      console.log("Calling Graph API:", graphConfig.graphMeEndpoint);
      
      const graphResponse = await fetch(graphConfig.graphMeEndpoint, {
        headers: {
          Authorization: `Bearer ${tokenResponse.accessToken}`,
        },
      });

      console.log("Graph API response status:", graphResponse.status);
      console.log("Graph API response statusText:", graphResponse.statusText);

      if (!graphResponse.ok) {
        const errorText = await graphResponse.text();
        console.error("Graph API error response:", errorText);
        throw new Error(`Failed to fetch user profile from Graph: ${graphResponse.status} ${graphResponse.statusText}`);
      }

      const graphData = await graphResponse.json();

      // Agora busca dados da nossa API usando o token correto
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
        setUser({
          id: graphData.id,
          displayName: graphData.displayName || "User",
          email: graphData.mail || graphData.userPrincipalName || "",
          department: graphData.department,
          jobTitle: graphData.jobTitle,
          role: UserRole.Employee,
        });
      }

      // Reset retry count on success
      setRetryCount(0);
    } catch (err) {
      console.error("Error fetching user profile:", err);
      const errorMessage = err instanceof Error ? err.message : "Failed to fetch user profile";
      setError(errorMessage);
      
      // Incrementa o contador de tentativas
      setRetryCount(prev => prev + 1);
      
      // Se ainda não atingiu o máximo e é um erro de autenticação, tenta novamente após um delay
      if (retryCount < maxRetries - 1 && errorMessage.includes("Invalid")) {
        console.log(`Tentativa ${retryCount + 1} de ${maxRetries} falhou. Tentando novamente em 2 segundos...`);
        setTimeout(() => {
          isRetryingRef.current = false;
          fetchUserProfile();
        }, 2000);
      } else if (accounts.length > 0) {
        // Fallback com dados básicos
        setUser({
          id: accounts[0].localAccountId,
          displayName: accounts[0].name || "User",
          email: accounts[0].username || "",
          role: UserRole.Employee,
        });
      }
    } finally {
      setLoading(false);
      isRetryingRef.current = false;
    }
  }, [accounts, callApi, instance, retryCount]);

  const refreshUser = async () => {
    setRetryCount(0); // Reset retry count when manually refreshing
    await fetchUserProfile();
  };

  const logout = () => {
    instance.logoutPopup().catch((error) => {
      console.error("Logout failed:", error);
      instance.logoutRedirect();
    });
  };

  useEffect(() => {
    if (inProgress === InteractionStatus.None) {
      fetchUserProfile();
    }
  }, [inProgress, fetchUserProfile]);

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
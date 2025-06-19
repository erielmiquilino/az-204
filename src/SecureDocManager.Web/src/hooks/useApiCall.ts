import { useMsal } from "@azure/msal-react";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { loginRequest, apiConfig } from "../config/authConfig";
import type { ApiResponse } from "../types";
import toast from "react-hot-toast";
import { useCallback, useMemo } from "react";

interface ApiCallOptions extends RequestInit {
  showError?: boolean;
  showSuccess?: boolean;
  successMessage?: string;
}

export const useApiCall = () => {
  const { instance, accounts } = useMsal();

  const acquireToken = useCallback(async () => {
    const request = {
      ...loginRequest,
      account: accounts[0],
    };

    try {
      const response = await instance.acquireTokenSilent(request);
      return response.accessToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        // Fallback to interactive method if silent fails
        const response = await instance.acquireTokenPopup(request);
        return response.accessToken;
      }
      throw error;
    }
  }, [accounts, instance]);

  const callApi = useCallback(async <T>(
    endpoint: string,
    options: ApiCallOptions = {}
  ): Promise<ApiResponse<T>> => {
    const {
      showError = true,
      showSuccess = false,
      successMessage = "Operação realizada com sucesso!",
      ...fetchOptions
    } = options;

    try {
      const token = await acquireToken();
      
      const url = endpoint.startsWith("http") 
        ? endpoint 
        : `${apiConfig.baseUrl}${endpoint}`;

      const response = await fetch(url, {
        ...fetchOptions,
        headers: {
          "Content-Type": "application/json",
          ...fetchOptions.headers,
          Authorization: `Bearer ${token}`,
        },
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error || `Erro: ${response.status} ${response.statusText}`;
        
        if (showError) {
          toast.error(errorMessage);
        }
        
        return {
          success: false,
          error: errorMessage,
          data: undefined,
        };
      }

      if (showSuccess) {
        toast.success(successMessage);
      }

      return {
        success: true,
        data,
      };
    } catch (error) {
      const errorMessage = error instanceof Error 
        ? error.message 
        : "Erro ao processar requisição";
      
      if (showError) {
        toast.error(errorMessage);
      }
      
      return {
        success: false,
        error: errorMessage,
      };
    }
  }, [acquireToken]);

  // Convenience methods
  const get = useCallback(<T>(endpoint: string, options?: ApiCallOptions) => {
    return callApi<T>(endpoint, { ...options, method: "GET" });
  }, [callApi]);

  const post = useCallback(<T, D = unknown>(endpoint: string, data: D, options?: ApiCallOptions) => {
    return callApi<T>(endpoint, {
      ...options,
      method: "POST",
      body: JSON.stringify(data),
    });
  }, [callApi]);

  const put = useCallback(<T, D = unknown>(endpoint: string, data: D, options?: ApiCallOptions) => {
    return callApi<T>(endpoint, {
      ...options,
      method: "PUT",
      body: JSON.stringify(data),
    });
  }, [callApi]);

  const del = useCallback(<T>(endpoint: string, options?: ApiCallOptions) => {
    return callApi<T>(endpoint, { ...options, method: "DELETE" });
  }, [callApi]);

  // Special method for file upload
  const uploadFile = useCallback(async <T>(
    endpoint: string,
    formData: FormData,
    options?: ApiCallOptions
  ): Promise<ApiResponse<T>> => {
    try {
      const token = await acquireToken();
      
      const url = endpoint.startsWith("http") 
        ? endpoint 
        : `${apiConfig.baseUrl}${endpoint}`;

      const response = await fetch(url, {
        ...options,
        method: "POST",
        headers: {
          ...options?.headers,
          Authorization: `Bearer ${token}`,
          // Don't set Content-Type for FormData
        },
        body: formData,
      });

      const data = await response.json();

      if (!response.ok) {
        const errorMessage = data.error || `Erro: ${response.status} ${response.statusText}`;
        
        if (options?.showError !== false) {
          toast.error(errorMessage);
        }
        
        return {
          success: false,
          error: errorMessage,
          data: undefined,
        };
      }

      if (options?.showSuccess) {
        toast.success(options.successMessage || "Arquivo enviado com sucesso!");
      }

      return {
        success: true,
        data,
      };
    } catch (error) {
      const errorMessage = error instanceof Error 
        ? error.message 
        : "Erro ao enviar arquivo";
      
      if (options?.showError !== false) {
        toast.error(errorMessage);
      }
      
      return {
        success: false,
        error: errorMessage,
      };
    }
  }, [acquireToken]);

  return useMemo(() => ({
    callApi,
    get,
    post,
    put,
    delete: del,
    uploadFile,
  }), [callApi, get, post, put, del, uploadFile]);
}; 
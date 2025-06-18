import { useApiCall } from "../hooks/useApiCall";
import type { DocumentFilterDto, Document, User, AuditLog } from "../types";

// Helper function to convert object to query string
const toQueryString = (obj: Record<string, unknown>): string => {
  const params = new URLSearchParams();
  Object.entries(obj).forEach(([key, value]) => {
    if (value !== undefined && value !== null) {
      params.append(key, String(value));
    }
  });
  return params.toString();
};

export const useApi = () => {
  const api = useApiCall();
  
  return {
    documents: {
      list: (filters?: DocumentFilterDto) => {
        const queryParams = filters ? `?${toQueryString(filters as Record<string, unknown>)}` : '';
        return api.get<Document[]>(`/documents${queryParams}`);
      },
      
      get: (id: string) => 
        api.get<Document>(`/documents/${id}`),
      
      upload: (formData: FormData) => 
        api.uploadFile<Document>("/documents", formData, {
          showSuccess: true,
          successMessage: "Documento enviado com sucesso!"
        }),
      
      update: (id: string, data: Partial<Document>) => 
        api.put<Document>(`/documents/${id}`, data, {
          showSuccess: true,
          successMessage: "Documento atualizado com sucesso!"
        }),
      
      delete: (id: string) => 
        api.delete<void>(`/documents/${id}`, {
          showSuccess: true,
          successMessage: "Documento excluído com sucesso!"
        }),
      
      download: (id: string) => 
        api.get<Blob>(`/documents/${id}/download`),
      
      sign: (id: string) => 
        api.post<Document>(`/documents/${id}/sign`, {}, {
          showSuccess: true,
          successMessage: "Documento assinado com sucesso!"
        }),
    },
    
    users: {
      list: () => 
        api.get<User[]>("/users"),
      
      get: (id: string) => 
        api.get<User>(`/users/${id}`),
      
      me: () => 
        api.get<User>("/users/me"),
      
      update: (id: string, data: Partial<User>) => 
        api.put<User>(`/users/${id}`, data, {
          showSuccess: true,
          successMessage: "Usuário atualizado com sucesso!"
        }),
    },
    
    audit: {
      list: (filters?: { userId?: string; documentId?: string; fromDate?: Date; toDate?: Date }) => {
        const queryParams = filters ? `?${toQueryString(filters as Record<string, unknown>)}` : '';
        return api.get<AuditLog[]>(`/audit${queryParams}`);
      },
      
      getByDocument: (documentId: string) => 
        api.get<AuditLog[]>(`/audit/document/${documentId}`),
      
      getByUser: (userId: string) => 
        api.get<AuditLog[]>(`/audit/user/${userId}`),
    },
  };
}; 
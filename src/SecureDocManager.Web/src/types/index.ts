// User types
export interface User {
  id: string;
  displayName: string;
  email: string;
  department?: string;
  jobTitle?: string;
  role: UserRole;
}

export const UserRole = {
  Admin: "Admin",
  Manager: "Manager",
  Employee: "Employee",
} as const;

export type UserRole = typeof UserRole[keyof typeof UserRole];

// Document types
export interface Document {
  id: number;
  fileName: string;
  fileExtension: string;
  fileSizeInBytes: number;
  departmentId: string;
  uploadedByUserName?: string;
  uploadedAt: string;
  description?: string;
  accessLevel: AccessLevel;
  isSigned: boolean;
  isDigitallySigned: boolean;
  tags: string[];
  downloadUrl?: string;
  contentType: string;
}

export const AccessLevel = {
  Public: 1,
  Internal: 2,
  Confidential: 3,
} as const;

export type AccessLevel = typeof AccessLevel[keyof typeof AccessLevel];

// Audit types
export interface AuditLog {
  id: string;
  userId: string;
  documentId: string;
  action: AuditAction;
  timestamp: Date;
  ipAddress?: string;
  details?: string;
}

export const AuditAction = {
  View: "View",
  Download: "Download",
  Upload: "Upload",
  Update: "Update",
  Delete: "Delete",
  Sign: "Sign",
} as const;

export type AuditAction = typeof AuditAction[keyof typeof AuditAction];

// DTO types
export interface DocumentUploadDto {
  file: File;
  title: string;
  description?: string;
  departmentId: string;
  tags?: string[];
  accessLevel: AccessLevel;
}

export interface DocumentFilterDto {
  departmentId?: string;
  uploadedBy?: string;
  tags?: string[];
  fromDate?: Date;
  toDate?: Date;
  accessLevel?: AccessLevel;
}

// API Response types
export interface ApiResponse<T> {
  data?: T;
  error?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// App State types
export interface AppState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

// Document Signature types
export interface DocumentSignature {
  id: string;
  documentId: number;
  signedBy: string;
  signedByName: string;
  signedByEmail: string;
  signedAt: Date;
  certificateThumbprint: string;
  isValid: boolean;
  verifiedAt?: Date;
}

export interface SignDocumentResponse {
  message: string;
  signatureId: string;
  signedAt: Date;
  certificateThumbprint: string;
}

export interface VerifySignatureResponse {
  isValid: boolean;
  signatureId: string;
  verifiedAt: Date;
}

export interface DocumentUploadResponse {
  documentId: number;
  message: string;
} 
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
  id: string;
  title: string;
  description?: string;
  fileName: string;
  fileSize: number;
  mimeType: string;
  uploadedBy: string;
  uploadedByName?: string;
  uploadDate: Date;
  departmentId: string;
  tags?: string[];
  accessLevel: AccessLevel;
  isDigitallySigned?: boolean;
  signedBy?: string;
  signedDate?: Date;
  downloadUrl?: string;
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
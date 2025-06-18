import React, { lazy, Suspense } from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import { CircularProgress, Box } from "@mui/material";
import { useAuth } from "../contexts/AuthContext";
import { UserRole } from "../types";

// Lazy load pages
const Dashboard = lazy(() => import("../pages/Dashboard"));
const Documents = lazy(() => import("../pages/Documents"));
const DocumentUpload = lazy(() => import("../pages/DocumentUpload"));
const Users = lazy(() => import("../pages/Users"));
const Audit = lazy(() => import("../pages/Audit"));
const Settings = lazy(() => import("../pages/Settings"));
const Profile = lazy(() => import("../pages/Profile"));
const NotFound = lazy(() => import("../pages/NotFound"));

// Loading component
const PageLoader = () => (
  <Box
    display="flex"
    justifyContent="center"
    alignItems="center"
    minHeight="60vh"
  >
    <CircularProgress />
  </Box>
);

// Protected Route component
interface ProtectedRouteProps {
  element: React.ReactElement;
  allowedRoles?: UserRole[];
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  element,
  allowedRoles,
}) => {
  const { user, loading } = useAuth();

  if (loading) {
    return <PageLoader />;
  }

  if (!user) {
    return <Navigate to="/" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/unauthorized" replace />;
  }

  return element;
};

const AppRoutes: React.FC = () => {
  return (
    <Suspense fallback={<PageLoader />}>
      <Routes>
        {/* Public routes */}
        <Route path="/" element={<Dashboard />} />
        
        {/* Protected routes - All users */}
        <Route
          path="/documents"
          element={
            <ProtectedRoute
              element={<Documents />}
              allowedRoles={[UserRole.Admin, UserRole.Manager, UserRole.Employee]}
            />
          }
        />
        <Route
          path="/upload"
          element={
            <ProtectedRoute
              element={<DocumentUpload />}
              allowedRoles={[UserRole.Admin, UserRole.Manager, UserRole.Employee]}
            />
          }
        />
        <Route
          path="/profile"
          element={
            <ProtectedRoute
              element={<Profile />}
              allowedRoles={[UserRole.Admin, UserRole.Manager, UserRole.Employee]}
            />
          }
        />
        
        {/* Protected routes - Admin and Manager */}
        <Route
          path="/audit"
          element={
            <ProtectedRoute
              element={<Audit />}
              allowedRoles={[UserRole.Admin, UserRole.Manager]}
            />
          }
        />
        
        {/* Protected routes - Admin only */}
        <Route
          path="/users"
          element={
            <ProtectedRoute
              element={<Users />}
              allowedRoles={[UserRole.Admin]}
            />
          }
        />
        <Route
          path="/settings"
          element={
            <ProtectedRoute
              element={<Settings />}
              allowedRoles={[UserRole.Admin]}
            />
          }
        />
        
        {/* Unauthorized route */}
        <Route
          path="/unauthorized"
          element={
            <Box p={4} textAlign="center">
              <h1>Acesso Negado</h1>
              <p>Você não tem permissão para acessar esta página.</p>
            </Box>
          }
        />
        
        {/* 404 route */}
        <Route path="*" element={<NotFound />} />
      </Routes>
    </Suspense>
  );
};

export default AppRoutes; 
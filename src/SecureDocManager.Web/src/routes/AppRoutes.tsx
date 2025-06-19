import React from 'react';
import { Route, Routes } from 'react-router-dom';

import Dashboard from '../pages/Dashboard';
import Documents from '../pages/Documents';
import DocumentUpload from '../pages/DocumentUpload';
import DocumentViewer from '../pages/DocumentViewer';
import Audit from '../pages/Audit';
import Users from '../pages/Users';
import Profile from '../pages/Profile';
import Settings from '../pages/Settings';
import NotFound from '../pages/NotFound';
import AuthError from '../pages/AuthError';

const AppRoutes: React.FC = () => {
  return (
    <Routes>
      <Route path="/" element={<Dashboard />} />
      <Route path="/documents" element={<Documents />} />
      <Route path="/documents/upload" element={<DocumentUpload />} />
      <Route path="/documents/:id" element={<DocumentViewer />} />
      <Route path="/audit" element={<Audit />} />
      <Route path="/users" element={<Users />} />
      <Route path="/profile" element={<Profile />} />
      <Route path="/settings" element={<Settings />} />
      <Route path="/auth-error" element={<AuthError />} />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
};

export default AppRoutes;

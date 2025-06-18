import * as React from 'react';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  Dashboard,
  Description as DocumentIcon,
  CloudUpload,
  People,
  History,
  Settings,
} from '@mui/icons-material';
import { useAuth } from '../../contexts/useAuth';
import { UserRole } from '../../types';

const MainListItems = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();

  const handleNavigate = (path: string) => {
    navigate(path);
  };

  const menuItems = [
    {
      text: 'Dashboard',
      icon: <Dashboard />,
      path: '/',
      roles: [UserRole.Admin, UserRole.Manager, UserRole.Employee],
    },
    {
      text: 'Documentos',
      icon: <DocumentIcon />,
      path: '/documents',
      roles: [UserRole.Admin, UserRole.Manager, UserRole.Employee],
    },
    {
      text: 'Upload',
      icon: <CloudUpload />,
      path: '/documents/upload',
      roles: [UserRole.Admin, UserRole.Manager, UserRole.Employee],
    },
    {
      text: 'Usuários',
      icon: <People />,
      path: '/users',
      roles: [UserRole.Admin],
    },
    {
      text: 'Auditoria',
      icon: <History />,
      path: '/audit',
      roles: [UserRole.Admin, UserRole.Manager],
    },
    {
      text: 'Configurações',
      icon: <Settings />,
      path: '/settings',
      roles: [UserRole.Admin],
    },
  ];

  const filteredMenuItems = menuItems.filter(
    (item) => user && item.roles.includes(user.role)
  );

  return (
    <React.Fragment>
      {filteredMenuItems.map((item) => (
        <ListItemButton
          key={item.text}
          selected={location.pathname === item.path}
          onClick={() => handleNavigate(item.path)}
        >
          <ListItemIcon>{item.icon}</ListItemIcon>
          <ListItemText primary={item.text} />
        </ListItemButton>
      ))}
    </React.Fragment>
  );
};

export default MainListItems; 
import React from 'react';
import { Box, Typography, Button, Container, Paper, Alert } from '@mui/material';
import { ErrorOutline } from '@mui/icons-material';
import { useMsal } from '@azure/msal-react';

const AuthError: React.FC = () => {
  const { instance } = useMsal();

  const handleRetry = async () => {
    try {
      await instance.clearCache();
      window.location.href = '/';
    } catch (error) {
      console.error('Failed to clear cache:', error);
    }
  };

  const handleLogout = async () => {
    try {
      await instance.logoutPopup();
    } catch {
      await instance.logoutRedirect();
    }
  };

  return (
    <Container maxWidth="sm" sx={{ mt: 8 }}>
      <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
        <Box sx={{ mb: 3 }}>
          <ErrorOutline sx={{ fontSize: 60, color: 'error.main' }} />
        </Box>
        
        <Typography variant="h4" gutterBottom>
          Erro de Autenticação
        </Typography>
        
        <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
          Não foi possível carregar seu perfil do Microsoft Graph.
        </Typography>

        <Alert severity="error" sx={{ mb: 3, textAlign: 'left' }}>
          <Typography variant="body2" gutterBottom>
            <strong>Possíveis causas:</strong>
          </Typography>
          <ul style={{ marginTop: 8, paddingLeft: 20 }}>
            <li>Permissões insuficientes no Azure AD</li>
            <li>Token de acesso expirado ou inválido</li>
            <li>Configuração incorreta da aplicação</li>
          </ul>
        </Alert>

        <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
          <Button
            variant="contained"
            color="primary"
            onClick={handleRetry}
          >
            Tentar Novamente
          </Button>
          
          <Button
            variant="outlined"
            color="secondary"
            onClick={handleLogout}
          >
            Fazer Logout
          </Button>
        </Box>

        <Typography variant="caption" color="text.secondary" sx={{ mt: 3, display: 'block' }}>
          Se o problema persistir, entre em contato com o administrador do sistema.
        </Typography>
      </Paper>
    </Container>
  );
};

export default AuthError; 
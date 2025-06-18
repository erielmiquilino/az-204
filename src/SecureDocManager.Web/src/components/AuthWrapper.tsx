import React from "react";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "../config/authConfig";
import { useAuth } from "../contexts/useAuth";
import { Navigate } from "react-router-dom";
import { 
  Box, 
  Button, 
  Container, 
  Typography, 
  Paper,
  CircularProgress,
  ThemeProvider,
  createTheme,
  CssBaseline
} from "@mui/material";
import { Security, Login } from "@mui/icons-material";

// Tema customizado para a aplicação
const theme = createTheme({
  palette: {
    primary: {
      main: "#0078d4", // Azure blue
    },
    secondary: {
      main: "#40e0d0",
    },
    background: {
      default: "#f5f5f5",
    },
  },
  typography: {
    h1: {
      fontSize: "2.5rem",
      fontWeight: 600,
    },
    h2: {
      fontSize: "2rem",
      fontWeight: 500,
    },
  },
});

interface AuthWrapperProps {
  children: React.ReactNode;
}

const AuthWrapper: React.FC<AuthWrapperProps> = ({ children }) => {
  const { instance, accounts, inProgress } = useMsal();
  const [isAuthenticated, setIsAuthenticated] = React.useState(false);

  React.useEffect(() => {
    setIsAuthenticated(accounts.length > 0);
  }, [accounts]);

  const handleLogin = () => {
    instance.loginPopup(loginRequest).catch((error) => {
      console.error("Login failed:", error);
    });
  };

  const handleLoginRedirect = () => {
    instance.loginRedirect(loginRequest).catch((error) => {
      console.error("Login redirect failed:", error);
    });
  };

  // Loading state
  if (inProgress === "login") {
    return (
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <Box
          display="flex"
          justifyContent="center"
          alignItems="center"
          minHeight="100vh"
          bgcolor="background.default"
        >
          <CircularProgress size={60} />
        </Box>
      </ThemeProvider>
    );
  }

  // Not authenticated
  if (!isAuthenticated) {
    return (
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <Box
          display="flex"
          justifyContent="center"
          alignItems="center"
          minHeight="100vh"
          bgcolor="background.default"
        >
          <Container maxWidth="sm">
            <Paper elevation={3} sx={{ p: 6, textAlign: "center" }}>
              <Security sx={{ fontSize: 80, color: "primary.main", mb: 3 }} />
              
              <Typography variant="h1" gutterBottom color="primary">
                SecureDocManager
              </Typography>
              
              <Typography variant="h6" color="textSecondary" paragraph>
                Sistema de Gestão de Documentos Corporativos
              </Typography>
              
              <Typography variant="body1" color="textSecondary" paragraph sx={{ mb: 4 }}>
                Faça login com sua conta corporativa Microsoft para acessar o sistema
              </Typography>

              <Box display="flex" flexDirection="column" gap={2} mt={4}>
                <Button
                  variant="contained"
                  size="large"
                  startIcon={<Login />}
                  onClick={handleLogin}
                  fullWidth
                  sx={{ py: 1.5 }}
                >
                  Login com Microsoft (Popup)
                </Button>
                
                <Button
                  variant="outlined"
                  size="large"
                  startIcon={<Login />}
                  onClick={handleLoginRedirect}
                  fullWidth
                  sx={{ py: 1.5 }}
                >
                  Login com Microsoft (Redirect)
                </Button>
              </Box>

              <Typography variant="caption" color="textSecondary" sx={{ mt: 4, display: "block" }}>
                Certificação AZ-204 - Implementação de Segurança Azure
              </Typography>
            </Paper>
          </Container>
        </Box>
      </ThemeProvider>
    );
  }

  // Authenticated - render children with auth check
  return <AuthenticatedWrapper>{children}</AuthenticatedWrapper>;
};

// Componente separado para quando já está autenticado
const AuthenticatedWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { error } = useAuth();

  if (error) {
    return <Navigate to="/auth-error" />;
  }

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </ThemeProvider>
  );
};

export default AuthWrapper; 
import React, { useEffect, useState } from "react";
import {
  Box,
  Paper,
  Typography,
  Card,
  CardContent,
  CircularProgress,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Chip,
  useTheme,
} from "@mui/material";
import {
  Description,
  CloudUpload,
  People,
  Security,
  TrendingUp,
  AccessTime,
  Folder,
  VpnKey,
} from "@mui/icons-material";
import { useAuth } from "../contexts/AuthContext";
import { useApiCall } from "../hooks/useApiCall";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import type { Document, AuditLog } from "../types";
import { AccessLevel } from "../types";

interface DashboardStats {
  totalDocuments: number;
  documentsThisMonth: number;
  totalUsers: number;
  recentActivities: AuditLog[];
  documentsByDepartment: { department: string; count: number }[];
  recentDocuments: Document[];
}

const Dashboard: React.FC = () => {
  const theme = useTheme();
  const { user } = useAuth();
  const { get } = useApiCall();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const response = await get<DashboardStats>("/dashboard/stats");
        if (response.success && response.data) {
          setStats(response.data);
        }
      } catch (error) {
        console.error("Error fetching dashboard data:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  const getAccessLevelColor = (level: AccessLevel) => {
    switch (level) {
      case AccessLevel.Confidential:
        return "error";
      case AccessLevel.Internal:
        return "warning";
      case AccessLevel.Public:
        return "success";
      default:
        return "default";
    }
  };

  const getAccessLevelLabel = (level: AccessLevel) => {
    switch (level) {
      case AccessLevel.Confidential:
        return "Confidencial";
      case AccessLevel.Internal:
        return "Interno";
      case AccessLevel.Public:
        return "Público";
      default:
        return "Desconhecido";
    }
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <CircularProgress />
      </Box>
    );
  }

  const statsCards = [
    {
      title: "Total de Documentos",
      value: stats?.totalDocuments || 0,
      icon: <Description />,
      color: theme.palette.primary.main,
    },
    {
      title: "Novos este Mês",
      value: stats?.documentsThisMonth || 0,
      icon: <CloudUpload />,
      color: theme.palette.success.main,
    },
    {
      title: "Usuários Ativos",
      value: stats?.totalUsers || 0,
      icon: <People />,
      color: theme.palette.info.main,
    },
    {
      title: "Nível de Segurança",
      value: "Alto",
      icon: <Security />,
      color: theme.palette.warning.main,
    },
  ];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Dashboard
      </Typography>
      
      <Typography variant="subtitle1" color="textSecondary" gutterBottom>
        Bem-vindo, {user?.displayName}! Aqui está um resumo da atividade do sistema.
      </Typography>

      {/* Stats Cards */}
      <Box 
        display="grid" 
        gridTemplateColumns={{ xs: "1fr", sm: "repeat(2, 1fr)", md: "repeat(4, 1fr)" }}
        gap={3}
        mt={3}
      >
        {statsCards.map((stat, index) => (
          <Card key={index}>
            <CardContent>
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography color="textSecondary" gutterBottom variant="body2">
                    {stat.title}
                  </Typography>
                  <Typography variant="h4">
                    {stat.value}
                  </Typography>
                </Box>
                <Box
                  sx={{
                    backgroundColor: `${stat.color}20`,
                    borderRadius: 2,
                    p: 1.5,
                    display: "flex",
                  }}
                >
                  {React.cloneElement(stat.icon, {
                    sx: { color: stat.color, fontSize: 30 },
                  })}
                </Box>
              </Box>
            </CardContent>
          </Card>
        ))}
      </Box>

      {/* Documents and Department Stats Row */}
      <Box 
        display="grid" 
        gridTemplateColumns={{ xs: "1fr", md: "2fr 1fr" }}
        gap={3}
        mt={3}
      >
        {/* Recent Documents */}
        <Paper sx={{ p: 3 }}>
          <Box display="flex" alignItems="center" mb={2}>
            <Folder sx={{ mr: 1, color: theme.palette.primary.main }} />
            <Typography variant="h6">Documentos Recentes</Typography>
          </Box>
          
          {stats?.recentDocuments && stats.recentDocuments.length > 0 ? (
            <List>
              {stats.recentDocuments.slice(0, 5).map((doc) => (
                <ListItem key={doc.id} divider>
                  <ListItemIcon>
                    <Description />
                  </ListItemIcon>
                  <ListItemText
                    primary={doc.title}
                    secondary={
                      <Box display="flex" alignItems="center" gap={1}>
                        <Typography variant="caption">
                          {format(new Date(doc.uploadDate), "dd/MM/yyyy", { locale: ptBR })}
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          • {doc.uploadedByName || doc.uploadedBy}
                        </Typography>
                      </Box>
                    }
                  />
                  <Chip
                    size="small"
                    label={getAccessLevelLabel(doc.accessLevel)}
                    color={getAccessLevelColor(doc.accessLevel)}
                  />
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography color="textSecondary" align="center" sx={{ py: 4 }}>
              Nenhum documento recente
            </Typography>
          )}
        </Paper>

        {/* Department Stats */}
        <Paper sx={{ p: 3 }}>
          <Box display="flex" alignItems="center" mb={2}>
            <TrendingUp sx={{ mr: 1, color: theme.palette.primary.main }} />
            <Typography variant="h6">Por Departamento</Typography>
          </Box>
          
          {stats?.documentsByDepartment && stats.documentsByDepartment.length > 0 ? (
            <List>
              {stats.documentsByDepartment.map((dept, index) => (
                <ListItem key={index}>
                  <ListItemText
                    primary={dept.department || "Sem departamento"}
                    secondary={`${dept.count} documentos`}
                  />
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography color="textSecondary" align="center" sx={{ py: 4 }}>
              Sem dados disponíveis
            </Typography>
          )}
        </Paper>
      </Box>

      {/* Recent Activities */}
      <Box mt={3}>
        <Paper sx={{ p: 3 }}>
          <Box display="flex" alignItems="center" mb={2}>
            <AccessTime sx={{ mr: 1, color: theme.palette.primary.main }} />
            <Typography variant="h6">Atividades Recentes</Typography>
          </Box>
          
          {stats?.recentActivities && stats.recentActivities.length > 0 ? (
            <List>
              {stats.recentActivities.slice(0, 10).map((activity) => (
                <ListItem key={activity.id} divider>
                  <ListItemText
                    primary={`${activity.action} - ${activity.documentId}`}
                    secondary={
                      <Box>
                        <Typography variant="caption">
                          {format(new Date(activity.timestamp), "dd/MM/yyyy HH:mm", { locale: ptBR })}
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          {" • "} Usuário: {activity.userId}
                        </Typography>
                      </Box>
                    }
                  />
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography color="textSecondary" align="center" sx={{ py: 4 }}>
              Nenhuma atividade recente
            </Typography>
          )}
        </Paper>
      </Box>

      {/* Security Info */}
      <Box mt={3}>
        <Paper
          sx={{
            p: 3,
            background: `linear-gradient(45deg, ${theme.palette.primary.main}20 30%, ${theme.palette.secondary.main}20 90%)`,
            border: `1px solid ${theme.palette.primary.main}40`,
          }}
        >
          <Box display="flex" alignItems="center" gap={2}>
            <VpnKey sx={{ fontSize: 40, color: theme.palette.primary.main }} />
            <Box>
              <Typography variant="h6">Segurança Azure</Typography>
              <Typography variant="body2" color="textSecondary">
                Este sistema utiliza Microsoft Entra ID para autenticação, Azure Key Vault para segredos,
                e Managed Identity para acesso seguro aos recursos. Certificação AZ-204.
              </Typography>
            </Box>
          </Box>
        </Paper>
      </Box>
    </Box>
  );
};

export default Dashboard; 
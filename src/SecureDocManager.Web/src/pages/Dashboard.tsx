import React, { useState, useEffect, useCallback } from "react";
import {
  Typography,
  Box,
  Paper,
  Card,
  CardContent,
  CircularProgress,
  Alert,
} from "@mui/material";
import {
  Description as DocumentIcon,
  People as PeopleIcon,
  History as HistoryIcon,
  TrendingUp as TrendingUpIcon,
} from "@mui/icons-material";
import { useApi } from "../services/api.service";
import type { AuditLog } from "../types";

interface DashboardStats {
  totalDocuments: number;
  totalUsers: number;
  recentActivities: number;
  documentsThisMonth: number;
  publicDocuments: number;
  internalDocuments: number;
  confidentialDocuments: number;
  signedDocuments: number;
}

const Dashboard: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats>({
    totalDocuments: 0,
    totalUsers: 0,
    recentActivities: 0,
    documentsThisMonth: 0,
    publicDocuments: 0,
    internalDocuments: 0,
    confidentialDocuments: 0,
    signedDocuments: 0,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [recentActivities, setRecentActivities] = useState<AuditLog[]>([]);

  const api = useApi();

  const loadDashboardData = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      // Carregar dados em paralelo
      const [documentsResponse, usersResponse, auditResponse] = await Promise.all([
        api.documents.list(),
        api.users.list(),
        api.audit.list(),
      ]);

      if (documentsResponse.success && documentsResponse.data) {
        const documents = documentsResponse.data;
        const now = new Date();
        const thisMonth = documents.filter(doc => {
          const uploadDate = new Date(doc.uploadDate);
          return uploadDate.getMonth() === now.getMonth() &&
                 uploadDate.getFullYear() === now.getFullYear();
        });

        setStats(prev => ({
          ...prev,
          totalDocuments: documents.length,
          documentsThisMonth: thisMonth.length,
          publicDocuments: documents.filter(doc => doc.accessLevel === 1).length,
          internalDocuments: documents.filter(doc => doc.accessLevel === 2).length,
          confidentialDocuments: documents.filter(doc => doc.accessLevel === 3).length,
          signedDocuments: documents.filter(doc => doc.isDigitallySigned).length,
        }));
      }

      if (usersResponse.success && usersResponse.data) {
        setStats(prev => ({
          ...prev,
          totalUsers: usersResponse.data!.length,
        }));
      }

      if (auditResponse.success && auditResponse.data) {
        const sortedActivities = auditResponse.data
          .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
          .slice(0, 5);
        
        setRecentActivities(sortedActivities);
        setStats(prev => ({
          ...prev,
          recentActivities: auditResponse.data!.length,
        }));
      }
    } catch (err) {
      setError("Erro ao carregar dados do dashboard");
      console.error(err);
    }

    setLoading(false);
  }, [api.audit, api.documents, api.users]);

  useEffect(() => {
    loadDashboardData();
  }, [loadDashboardData]);

  const StatCard = ({ title, value, icon, color }: {
    title: string;
    value: number | string;
    icon: React.ReactNode;
    color: string;
  }) => (
    <Card sx={{ height: "100%" }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography color="textSecondary" gutterBottom variant="body2">
              {title}
            </Typography>
            <Typography variant="h4">
              {value}
            </Typography>
          </Box>
          <Box sx={{ color }}>
            {icon}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );

  const getActionLabel = (action: string) => {
    const labels: Record<string, string> = {
      View: "visualizou",
      Download: "baixou",
      Upload: "enviou",
      Update: "atualizou",
      Delete: "excluiu",
      Sign: "assinou",
    };
    return labels[action] || action;
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Dashboard
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Cards de estatísticas */}
      <Box
        display="grid"
        gridTemplateColumns={{
          xs: "1fr",
          sm: "repeat(2, 1fr)",
          md: "repeat(4, 1fr)",
        }}
        gap={3}
        mb={3}
      >
        <StatCard
          title="Total de Documentos"
          value={stats.totalDocuments}
          icon={<DocumentIcon fontSize="large" />}
          color="#1976d2"
        />
        <StatCard
          title="Total de Usuários"
          value={stats.totalUsers}
          icon={<PeopleIcon fontSize="large" />}
          color="#388e3c"
        />
        <StatCard
          title="Atividades Recentes"
          value={stats.recentActivities}
          icon={<HistoryIcon fontSize="large" />}
          color="#f57c00"
        />
        <StatCard
          title="Docs Este Mês"
          value={stats.documentsThisMonth}
          icon={<TrendingUpIcon fontSize="large" />}
          color="#7b1fa2"
        />
      </Box>

      {/* Estatísticas de documentos */}
      <Box
        display="grid"
        gridTemplateColumns={{
          xs: "1fr",
          md: "repeat(2, 1fr)",
        }}
        gap={3}
        mb={3}
      >
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Documentos por Nível de Acesso
          </Typography>
          <Box sx={{ mt: 2 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
              <Typography variant="body2">Públicos</Typography>
              <Typography variant="body2" fontWeight="bold">{stats.publicDocuments}</Typography>
            </Box>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
              <Typography variant="body2">Internos</Typography>
              <Typography variant="body2" fontWeight="bold">{stats.internalDocuments}</Typography>
            </Box>
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Typography variant="body2">Confidenciais</Typography>
              <Typography variant="body2" fontWeight="bold">{stats.confidentialDocuments}</Typography>
            </Box>
          </Box>
        </Paper>

        <Paper sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Status de Assinatura
          </Typography>
          <Box sx={{ mt: 2 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
              <Typography variant="body2">Documentos Assinados</Typography>
              <Typography variant="body2" fontWeight="bold" color="success.main">
                {stats.signedDocuments}
              </Typography>
            </Box>
            <Box display="flex" justifyContent="space-between" alignItems="center">
              <Typography variant="body2">Não Assinados</Typography>
              <Typography variant="body2" fontWeight="bold" color="text.secondary">
                {stats.totalDocuments - stats.signedDocuments}
              </Typography>
            </Box>
            {stats.totalDocuments > 0 && (
              <Box mt={2}>
                <Typography variant="body2" color="text.secondary">
                  {((stats.signedDocuments / stats.totalDocuments) * 100).toFixed(1)}% dos documentos estão assinados
                </Typography>
              </Box>
            )}
          </Box>
        </Paper>
      </Box>

      {/* Atividades recentes */}
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Atividades Recentes
        </Typography>
        {recentActivities.length > 0 ? (
          <Box sx={{ mt: 2 }}>
            {recentActivities.map((activity) => (
              <Box key={activity.id} sx={{ mb: 2, pb: 2, borderBottom: "1px solid #eee" }}>
                <Typography variant="body2">
                  <strong>Usuário {activity.userId}</strong> {getActionLabel(activity.action)} o documento{" "}
                  <strong>{activity.documentId}</strong>
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {new Date(activity.timestamp).toLocaleString("pt-BR")}
                  {activity.ipAddress && ` • IP: ${activity.ipAddress}`}
                </Typography>
              </Box>
            ))}
          </Box>
        ) : (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
            Nenhuma atividade recente
          </Typography>
        )}
      </Paper>
    </Box>
  );
};

export default Dashboard; 
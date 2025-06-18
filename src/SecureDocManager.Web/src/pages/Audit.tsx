import React, { useState, useEffect, useCallback } from "react";
import {
  Typography,
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  CircularProgress,
  Alert,
  TextField,
  Button,
  Stack,
  Tooltip,
} from "@mui/material";
import {
  Visibility as ViewIcon,
  Download as DownloadIcon,
  Upload as UploadIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  VerifiedUser as SignIcon,
  FilterList as FilterIcon,
} from "@mui/icons-material";
import { useApi } from "../services/api.service";
import type { AuditLog, AuditAction } from "../types";

const Audit: React.FC = () => {
  const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterOpen, setFilterOpen] = useState(false);
  const [filters, setFilters] = useState({
    userId: "",
    documentId: "",
    fromDate: "",
    toDate: "",
  });

  const api = useApi();

  const loadAuditLogs = useCallback(async () => {
    setLoading(true);
    setError(null);

    const hasFilters = filters.userId || filters.documentId || filters.fromDate || filters.toDate;
    
    const response = await api.audit.list(
      hasFilters
        ? {
            userId: filters.userId || undefined,
            documentId: filters.documentId || undefined,
            fromDate: filters.fromDate ? new Date(filters.fromDate) : undefined,
            toDate: filters.toDate ? new Date(filters.toDate) : undefined,
          }
        : undefined
    );

    if (response.success && response.data) {
      setAuditLogs(response.data);
    } else {
      setError(response.error || "Erro ao carregar logs de auditoria");
    }

    setLoading(false);
  }, [api.audit, filters.documentId, filters.fromDate, filters.toDate, filters.userId]);

  useEffect(() => {
    loadAuditLogs();
  }, [loadAuditLogs]);

  const getActionIcon = (action: AuditAction) => {
    switch (action) {
      case "View":
        return <ViewIcon fontSize="small" />;
      case "Download":
        return <DownloadIcon fontSize="small" />;
      case "Upload":
        return <UploadIcon fontSize="small" />;
      case "Update":
        return <EditIcon fontSize="small" />;
      case "Delete":
        return <DeleteIcon fontSize="small" />;
      case "Sign":
        return <SignIcon fontSize="small" />;
      default:
        return null;
    }
  };

  const getActionColor = (action: AuditAction) => {
    switch (action) {
      case "View":
        return "info";
      case "Download":
        return "primary";
      case "Upload":
        return "success";
      case "Update":
        return "warning";
      case "Delete":
        return "error";
      case "Sign":
        return "success";
      default:
        return "default" as const;
    }
  };

  const getActionLabel = (action: AuditAction) => {
    switch (action) {
      case "View":
        return "Visualização";
      case "Download":
        return "Download";
      case "Upload":
        return "Upload";
      case "Update":
        return "Atualização";
      case "Delete":
        return "Exclusão";
      case "Sign":
        return "Assinatura";
      default:
        return action;
    }
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
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" gutterBottom>
          Auditoria
        </Typography>
        <Button
          variant="outlined"
          startIcon={<FilterIcon />}
          onClick={() => setFilterOpen(!filterOpen)}
        >
          Filtros
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {filterOpen && (
        <Paper sx={{ p: 2, mb: 2 }}>
          <Stack direction="row" spacing={2} alignItems="center">
            <TextField
              label="ID do Usuário"
              value={filters.userId}
              onChange={(e) => setFilters({ ...filters, userId: e.target.value })}
              size="small"
              sx={{ minWidth: 200 }}
            />
            <TextField
              label="ID do Documento"
              value={filters.documentId}
              onChange={(e) => setFilters({ ...filters, documentId: e.target.value })}
              size="small"
              sx={{ minWidth: 200 }}
            />
            <TextField
              label="Data Inicial"
              type="date"
              value={filters.fromDate}
              onChange={(e) => setFilters({ ...filters, fromDate: e.target.value })}
              size="small"
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 200 }}
            />
            <TextField
              label="Data Final"
              type="date"
              value={filters.toDate}
              onChange={(e) => setFilters({ ...filters, toDate: e.target.value })}
              size="small"
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 200 }}
            />
            <Button variant="contained" onClick={loadAuditLogs}>
              Aplicar
            </Button>
            <Button
              variant="outlined"
              onClick={() => {
                setFilters({
                  userId: "",
                  documentId: "",
                  fromDate: "",
                  toDate: "",
                });
                loadAuditLogs();
              }}
            >
              Limpar
            </Button>
          </Stack>
        </Paper>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Data/Hora</TableCell>
              <TableCell>Usuário</TableCell>
              <TableCell>Documento</TableCell>
              <TableCell>Ação</TableCell>
              <TableCell>IP</TableCell>
              <TableCell>Detalhes</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {auditLogs.map((log) => (
              <TableRow key={log.id}>
                <TableCell>
                  {new Date(log.timestamp).toLocaleString("pt-BR")}
                </TableCell>
                <TableCell>
                  <Tooltip title={`ID: ${log.userId}`}>
                    <span>{log.userId}</span>
                  </Tooltip>
                </TableCell>
                <TableCell>
                  <Tooltip title={`ID: ${log.documentId}`}>
                    <span>{log.documentId}</span>
                  </Tooltip>
                </TableCell>
                <TableCell>
                  <Stack direction="row" spacing={1} alignItems="center">
                    {getActionIcon(log.action)}
                    <Chip
                      label={getActionLabel(log.action)}
                      color={getActionColor(log.action)}
                      size="small"
                    />
                  </Stack>
                </TableCell>
                <TableCell>{log.ipAddress || "-"}</TableCell>
                <TableCell>{log.details || "-"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {auditLogs.length === 0 && !loading && (
        <Box textAlign="center" py={4}>
          <Typography color="textSecondary">
            Nenhum log de auditoria encontrado
          </Typography>
        </Box>
      )}
    </Box>
  );
};

export default Audit; 
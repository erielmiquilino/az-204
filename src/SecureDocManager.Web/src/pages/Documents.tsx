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
  IconButton,
  Chip,
  CircularProgress,
  Alert,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Stack,
} from "@mui/material";
import {
  Download as DownloadIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  Upload as UploadIcon,
  VerifiedUser as SignIcon,
} from "@mui/icons-material";
import { useApi } from "../services/api.service";
import type { Document, AccessLevel } from "../types";

const Documents: React.FC = () => {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [selectedDocument, setSelectedDocument] = useState<Document | null>(null);
  const [filterOpen, setFilterOpen] = useState(false);
  const [filters, setFilters] = useState({
    departmentId: "",
    accessLevel: "" as AccessLevel | "",
  });

  const api = useApi();

  const loadDocuments = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    const response = await api.documents.list(
      filters.departmentId || filters.accessLevel
        ? {
            departmentId: filters.departmentId || undefined,
            accessLevel: filters.accessLevel || undefined,
          }
        : undefined
    );

    if (response.success && response.data) {
      setDocuments(response.data);
    } else {
      setError(response.error || "Erro ao carregar documentos");
    }
    
    setLoading(false);
  }, [api.documents, filters.accessLevel, filters.departmentId]);

  useEffect(() => {
    loadDocuments();
  }, [loadDocuments]);

  const handleDownload = async (doc: Document) => {
    const response = await api.documents.download(doc.id);
    
    if (response.success && response.data) {
      // Criar um blob e fazer download
      const blob = response.data;
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = doc.fileName;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    }
  };

  const handleDelete = async () => {
    if (!selectedDocument) return;

    const response = await api.documents.delete(selectedDocument.id);
    
    if (response.success) {
      setDocuments(documents.filter(doc => doc.id !== selectedDocument.id));
      setDeleteDialogOpen(false);
      setSelectedDocument(null);
    }
  };

  const handleSign = async (doc: Document) => {
    const response = await api.documents.sign(doc.id);
    
    if (response.success && response.data) {
      // Atualizar o documento na lista
      setDocuments(documents.map(item => 
        item.id === doc.id ? response.data! : item
      ));
    }
  };

  const getAccessLevelColor = (level: AccessLevel) => {
    switch (level) {
      case 1: // Public
        return "success";
      case 2: // Internal
        return "warning";
      case 3: // Confidential
        return "error";
      default:
        return "default";
    }
  };

  const getAccessLevelLabel = (level: AccessLevel) => {
    switch (level) {
      case 1:
        return "Público";
      case 2:
        return "Interno";
      case 3:
        return "Confidencial";
      default:
        return "Desconhecido";
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return "0 Bytes";
    const k = 1024;
    const sizes = ["Bytes", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
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
          Documentos
        </Typography>
        <Box>
          <Button
            variant="outlined"
            onClick={() => setFilterOpen(!filterOpen)}
            sx={{ mr: 2 }}
          >
            Filtros
          </Button>
          <Button
            variant="contained"
            startIcon={<UploadIcon />}
            href="/documents/upload"
          >
            Novo Documento
          </Button>
        </Box>
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
              label="Departamento"
              value={filters.departmentId}
              onChange={(e) => setFilters({ ...filters, departmentId: e.target.value })}
              size="small"
              sx={{ minWidth: 200 }}
            />
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel>Nível de Acesso</InputLabel>
              <Select
                value={filters.accessLevel}
                onChange={(e) => setFilters({ ...filters, accessLevel: e.target.value as AccessLevel | "" })}
                label="Nível de Acesso"
              >
                <MenuItem value="">Todos</MenuItem>
                <MenuItem value={1}>Público</MenuItem>
                <MenuItem value={2}>Interno</MenuItem>
                <MenuItem value={3}>Confidencial</MenuItem>
              </Select>
            </FormControl>
            <Button variant="contained" onClick={loadDocuments}>
              Aplicar
            </Button>
            <Button
              variant="outlined"
              onClick={() => {
                setFilters({ departmentId: "", accessLevel: "" });
                loadDocuments();
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
              <TableCell>Título</TableCell>
              <TableCell>Arquivo</TableCell>
              <TableCell>Tamanho</TableCell>
              <TableCell>Enviado por</TableCell>
              <TableCell>Data de Upload</TableCell>
              <TableCell>Nível de Acesso</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="center">Ações</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {documents.map((document) => (
              <TableRow key={document.id}>
                <TableCell>{document.title}</TableCell>
                <TableCell>{document.fileName}</TableCell>
                <TableCell>{formatFileSize(document.fileSize)}</TableCell>
                <TableCell>{document.uploadedByName || document.uploadedBy}</TableCell>
                <TableCell>
                  {new Date(document.uploadDate).toLocaleDateString("pt-BR")}
                </TableCell>
                <TableCell>
                  <Chip
                    label={getAccessLevelLabel(document.accessLevel)}
                    color={getAccessLevelColor(document.accessLevel)}
                    size="small"
                  />
                </TableCell>
                <TableCell>
                  {document.isDigitallySigned ? (
                    <Chip
                      label="Assinado"
                      color="success"
                      size="small"
                      icon={<SignIcon />}
                    />
                  ) : (
                    <Chip label="Não assinado" color="default" size="small" />
                  )}
                </TableCell>
                <TableCell align="center">
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={() => window.open(`/documents/${document.id}`, "_blank")}
                    title="Visualizar"
                  >
                    <ViewIcon />
                  </IconButton>
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={() => handleDownload(document)}
                    title="Download"
                  >
                    <DownloadIcon />
                  </IconButton>
                  {!document.isDigitallySigned && (
                    <IconButton
                      size="small"
                      color="success"
                      onClick={() => handleSign(document)}
                      title="Assinar digitalmente"
                    >
                      <SignIcon />
                    </IconButton>
                  )}
                  <IconButton
                    size="small"
                    color="error"
                    onClick={() => {
                      setSelectedDocument(document);
                      setDeleteDialogOpen(true);
                    }}
                    title="Excluir"
                  >
                    <DeleteIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Dialog de confirmação de exclusão */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Confirmar Exclusão</DialogTitle>
        <DialogContent>
          <Typography>
            Tem certeza que deseja excluir o documento "{selectedDocument?.title}"?
          </Typography>
          <Typography variant="body2" color="textSecondary" sx={{ mt: 1 }}>
            Esta ação não pode ser desfeita.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancelar</Button>
          <Button onClick={handleDelete} color="error" variant="contained">
            Excluir
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default Documents; 
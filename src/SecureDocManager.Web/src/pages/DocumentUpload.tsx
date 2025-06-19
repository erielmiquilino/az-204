import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Typography,
  Box,
  Paper,
  TextField,
  Button,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Stack,
  Alert,
  CircularProgress,
  Input,
  FormHelperText,
} from "@mui/material";
import {
  CloudUpload as UploadIcon,
  AttachFile as FileIcon,
} from "@mui/icons-material";
import { useApi } from "../services/api.service";
import type { AccessLevel } from "../types";

const DocumentUpload: React.FC = () => {
  const navigate = useNavigate();
  const api = useApi();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  
  const [formData, setFormData] = useState({
    description: "",
    departmentId: "",
    accessLevel: 2 as AccessLevel, // Default to Internal
    tags: [] as string[],
  });
  
  const [file, setFile] = useState<File | null>(null);
  const [tagInput, setTagInput] = useState("");

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files[0]) {
      setFile(event.target.files[0]);
    }
  };

  const handleAddTag = () => {
    if (tagInput.trim() && !formData.tags.includes(tagInput.trim())) {
      setFormData({
        ...formData,
        tags: [...formData.tags, tagInput.trim()],
      });
      setTagInput("");
    }
  };

  const handleRemoveTag = (tagToRemove: string) => {
    setFormData({
      ...formData,
      tags: formData.tags.filter(tag => tag !== tagToRemove),
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!file) {
      setError("Por favor, selecione um arquivo");
      return;
    }

    setLoading(true);
    setError(null);

    const uploadData = new FormData();
    uploadData.append("file", file);
    uploadData.append("description", formData.description);
    uploadData.append("departmentId", formData.departmentId);
    uploadData.append("accessLevel", formData.accessLevel.toString());
    
    // Adicionar tags como string separada por vírgula
    if (formData.tags.length > 0) {
      uploadData.append("tags", formData.tags.join(","));
    }

    const response = await api.documents.upload(uploadData);

    if (response.success) {
      setSuccess(true);
      setTimeout(() => {
        navigate("/documents");
      }, 2000);
    } else {
      setError(response.error || "Erro ao fazer upload do documento");
    }

    setLoading(false);
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Upload de Documento
      </Typography>

      <Paper sx={{ p: 4, maxWidth: 800, mx: "auto" }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {success && (
          <Alert severity="success" sx={{ mb: 2 }}>
            Documento enviado com sucesso! Redirecionando...
          </Alert>
        )}

        <form onSubmit={handleSubmit}>
          <Stack spacing={3}>
            {/* Seleção de arquivo */}
            <FormControl>
              <Input
                id="file-upload"
                type="file"
                onChange={handleFileChange}
                style={{ display: "none" }}
                inputProps={{ accept: ".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.jpg,.jpeg,.png" }}
              />
              <label htmlFor="file-upload">
                <Button
                  variant="outlined"
                  component="span"
                  startIcon={<FileIcon />}
                  fullWidth
                  sx={{ justifyContent: "flex-start", py: 2 }}
                >
                  {file ? file.name : "Selecionar arquivo"}
                </Button>
              </label>
              {file && (
                <FormHelperText>
                  Tamanho: {(file.size / 1024 / 1024).toFixed(2)} MB
                </FormHelperText>
              )}
            </FormControl>

            {/* Descrição */}
            <TextField
              label="Descrição"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              multiline
              rows={3}
              fullWidth
            />

            {/* Departamento */}
            <TextField
              label="ID do Departamento"
              value={formData.departmentId}
              onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
              required
              fullWidth
              helperText="Entre com o ID do departamento"
            />

            {/* Nível de Acesso */}
            <FormControl fullWidth required>
              <InputLabel>Nível de Acesso</InputLabel>
              <Select
                value={formData.accessLevel}
                onChange={(e) => setFormData({ ...formData, accessLevel: e.target.value as AccessLevel })}
                label="Nível de Acesso"
              >
                <MenuItem value={1}>Público</MenuItem>
                <MenuItem value={2}>Interno</MenuItem>
                <MenuItem value={3}>Confidencial</MenuItem>
              </Select>
              <FormHelperText>
                {formData.accessLevel === 1 && "Todos podem acessar este documento"}
                {formData.accessLevel === 2 && "Apenas usuários internos podem acessar"}
                {formData.accessLevel === 3 && "Apenas usuários autorizados podem acessar"}
              </FormHelperText>
            </FormControl>

            {/* Tags */}
            <Box>
              <Stack direction="row" spacing={2} alignItems="center" mb={1}>
                <TextField
                  label="Adicionar tag"
                  value={tagInput}
                  onChange={(e) => setTagInput(e.target.value)}
                  onKeyPress={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      handleAddTag();
                    }
                  }}
                  size="small"
                  fullWidth
                />
                <Button onClick={handleAddTag} variant="outlined">
                  Adicionar
                </Button>
              </Stack>
              {formData.tags.length > 0 && (
                <Stack direction="row" spacing={1} flexWrap="wrap">
                  {formData.tags.map((tag) => (
                    <Chip
                      key={tag}
                      label={tag}
                      onDelete={() => handleRemoveTag(tag)}
                      size="small"
                      sx={{ mb: 1 }}
                    />
                  ))}
                </Stack>
              )}
            </Box>

            {/* Botões de ação */}
            <Stack direction="row" spacing={2} justifyContent="flex-end">
              <Button
                variant="outlined"
                onClick={() => navigate("/documents")}
                disabled={loading}
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                variant="contained"
                startIcon={loading ? <CircularProgress size={20} /> : <UploadIcon />}
                disabled={loading || !file || !formData.departmentId}
              >
                {loading ? "Enviando..." : "Enviar Documento"}
              </Button>
            </Stack>
          </Stack>
        </form>
      </Paper>
    </Box>
  );
};

export default DocumentUpload; 
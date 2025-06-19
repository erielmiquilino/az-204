import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useApi } from '../services/api.service';
import { Box, CircularProgress, Typography, Alert, Button } from '@mui/material';

const DocumentViewer: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [documentUrl, setDocumentUrl] = useState<string | null>(null);
  const [contentType, setContentType] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const api = useApi();

  useEffect(() => {
    const fetchDocument = async () => {
      if (!id) return;

      setLoading(true);
      
      // Primeiro, obtemos os metadados do documento para saber o tipo
      const metaResponse = await api.documents.get(id);

      if (!metaResponse.success || !metaResponse.data) {
        setError('Documento não encontrado ou acesso negado.');
        setLoading(false);
        return;
      }
      
      const doc = metaResponse.data;
      setContentType(doc.contentType);

      // Agora, baixamos o conteúdo do blob
      const blobResponse = await api.documents.download(id);

      if (blobResponse.success && blobResponse.data) {
        const url = window.URL.createObjectURL(blobResponse.data);
        setDocumentUrl(url);
      } else {
        setError('Falha ao carregar o conteúdo do documento.');
      }
      
      setLoading(false);
    };

    fetchDocument();

    // Cleanup
    return () => {
      if (documentUrl) {
        window.URL.revokeObjectURL(documentUrl);
      }
    };
  }, [id, api]);

  const renderContent = () => {
    if (!contentType || !documentUrl) {
      return (
        <Alert severity="warning">
          Não foi possível carregar o conteúdo para visualização.
        </Alert>
      );
    }
    
    if (contentType.startsWith('image/')) {
      return <img src={documentUrl} alt="Visualização do documento" style={{ maxWidth: '100%', maxHeight: '80vh' }} />;
    }
    
    if (contentType === 'application/pdf') {
      return <iframe src={documentUrl} width="100%" height="800px" title="Visualizador de PDF" />;
    }
    
    if (contentType === 'text/plain') {
        return (
            <iframe src={documentUrl} width="100%" height="800px" title="Visualizador de Texto" />
        );
    }

    return (
      <Alert severity="info">
        A visualização para este tipo de arquivo ({contentType}) não é suportada.
        <Button href={documentUrl} download sx={{ ml: 2 }} variant="contained">
          Baixar Arquivo
        </Button>
      </Alert>
    );
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        Visualizador de Documento
      </Typography>
      {error ? (
        <Alert severity="error">{error}</Alert>
      ) : (
        <Box mt={2}>{renderContent()}</Box>
      )}
    </Box>
  );
};

export default DocumentViewer; 
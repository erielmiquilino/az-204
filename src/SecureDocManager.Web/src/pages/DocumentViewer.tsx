import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useApi } from '../services/api.service';
import { Box, CircularProgress, Typography, Alert, Button } from '@mui/material';

// Helper para determinar o contentType baseado na extensão do arquivo
const getContentTypeFromExtension = (extension: string): string => {
  const ext = extension.toLowerCase();
  switch (ext) {
    case '.pdf':
      return 'application/pdf';
    case '.txt':
      return 'text/plain';
    case '.jpg':
    case '.jpeg':
      return 'image/jpeg';
    case '.png':
      return 'image/png';
    case '.gif':
      return 'image/gif';
    default:
      return 'application/octet-stream';
  }
};

const DocumentViewer: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [documentUrl, setDocumentUrl] = useState<string | null>(null);
  const [contentType, setContentType] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const api = useApi();

  // Efeito 1: Buscar metadados e contentType
  useEffect(() => {
    const fetchMetadata = async () => {
      if (!id) return;
      
      setLoading(true);
      setError(null);
      
      const metaResponse = await api.documents.get(id);

      if (metaResponse.success && metaResponse.data) {
        const doc = metaResponse.data;
        // Usar contentType do backend se disponível, senão derivar da extensão
        const docContentType = doc.contentType || getContentTypeFromExtension(doc.fileExtension);
        setContentType(docContentType);
        console.log('Metadados recebidos:', doc);
        console.log('ContentType determinado:', docContentType);
      } else {
        setError('Documento não encontrado ou acesso negado.');
        setLoading(false);
      }
    };

    fetchMetadata();
  }, [id, api]);

  // Efeito 2: Buscar o conteúdo do blob QUANDO o contentType estiver disponível
  useEffect(() => {
    const fetchContent = async () => {
      if (!id || !contentType) return;

      const blobResponse = await api.documents.download(id);
      console.log('Resposta do Blob:', blobResponse);

      if (blobResponse.success && blobResponse.data) {
        const url = window.URL.createObjectURL(blobResponse.data);
        console.log('URL do Blob criada:', url);
        setDocumentUrl(url);
      } else {
        setError('Falha ao carregar o conteúdo do documento.');
      }
      setLoading(false);
    };
    
    fetchContent();
    
    // Cleanup
    return () => {
      if (documentUrl) {
        window.URL.revokeObjectURL(documentUrl);
      }
    };
  }, [id, api, contentType, documentUrl]); // Depende do contentType

  const renderContent = () => {
    console.log('Renderizando conteúdo com:', { contentType, documentUrl });
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
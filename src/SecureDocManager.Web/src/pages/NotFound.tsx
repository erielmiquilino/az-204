import React from "react";
import { Box, Typography, Button } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { SentimentDissatisfied } from "@mui/icons-material";

const NotFound: React.FC = () => {
  const navigate = useNavigate();

  return (
    <Box
      display="flex"
      flexDirection="column"
      justifyContent="center"
      alignItems="center"
      minHeight="60vh"
      textAlign="center"
    >
      <SentimentDissatisfied sx={{ fontSize: 100, color: "text.secondary", mb: 2 }} />
      <Typography variant="h1" gutterBottom>
        404
      </Typography>
      <Typography variant="h5" color="textSecondary" gutterBottom>
        Página não encontrada
      </Typography>
      <Typography variant="body1" color="textSecondary" paragraph>
        A página que você está procurando não existe ou foi movida.
      </Typography>
      <Button
        variant="contained"
        onClick={() => navigate("/")}
        sx={{ mt: 2 }}
      >
        Voltar ao Dashboard
      </Button>
    </Box>
  );
};

export default NotFound; 
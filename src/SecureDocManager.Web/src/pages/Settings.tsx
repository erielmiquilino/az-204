import React from "react";
import { Typography } from "@mui/material";

const Settings: React.FC = () => {
  return (
    <div>
      <Typography variant="h4" gutterBottom>
        Configurações
      </Typography>
      <Typography variant="body1" color="textSecondary">
        Configurações do sistema serão gerenciadas aqui.
      </Typography>
    </div>
  );
};

export default Settings; 
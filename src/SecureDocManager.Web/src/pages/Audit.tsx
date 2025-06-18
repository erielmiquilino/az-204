import React from "react";
import { Typography } from "@mui/material";

const Audit: React.FC = () => {
  return (
    <div>
      <Typography variant="h4" gutterBottom>
        Auditoria
      </Typography>
      <Typography variant="body1" color="textSecondary">
        Logs de auditoria serão exibidos aqui.
      </Typography>
    </div>
  );
};

export default Audit; 
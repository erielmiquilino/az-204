import React from "react";
import { Typography } from "@mui/material";

const Profile: React.FC = () => {
  return (
    <div>
      <Typography variant="h4" gutterBottom>
        Meu Perfil
      </Typography>
      <Typography variant="body1" color="textSecondary">
        Informações do perfil do usuário serão exibidas aqui.
      </Typography>
    </div>
  );
};

export default Profile; 
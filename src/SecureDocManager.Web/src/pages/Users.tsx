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
  Avatar,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Button,
} from "@mui/material";
import {
  Edit as EditIcon,
  Person as PersonIcon,
} from "@mui/icons-material";
import { useApi } from "../services/api.service";
import type { User, UserRole } from "../types";

const Users: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [editForm, setEditForm] = useState({
    displayName: "",
    department: "",
    jobTitle: "",
    role: "" as UserRole,
  });

  const api = useApi();

  const loadUsers = useCallback(async () => {
    setLoading(true);
    setError(null);

    const response = await api.users.list();

    if (response.success && response.data) {
      setUsers(response.data);
    } else {
      setError(response.error || "Erro ao carregar usuários");
    }

    setLoading(false);
  }, [api.users]);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const handleEdit = (user: User) => {
    setSelectedUser(user);
    setEditForm({
      displayName: user.displayName,
      department: user.department || "",
      jobTitle: user.jobTitle || "",
      role: user.role,
    });
    setEditDialogOpen(true);
  };

  const handleSaveEdit = async () => {
    if (!selectedUser) return;

    const response = await api.users.update(selectedUser.id, editForm);

    if (response.success && response.data) {
      setUsers(users.map(user => 
        user.id === selectedUser.id ? response.data! : user
      ));
      setEditDialogOpen(false);
      setSelectedUser(null);
    }
  };

  const getRoleColor = (role: UserRole) => {
    switch (role) {
      case "Admin":
        return "error";
      case "Manager":
        return "warning";
      case "Employee":
        return "primary";
      default:
        return "default";
    }
  };

  const getRoleLabel = (role: UserRole) => {
    switch (role) {
      case "Admin":
        return "Administrador";
      case "Manager":
        return "Gerente";
      case "Employee":
        return "Funcionário";
      default:
        return role;
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
      <Typography variant="h4" gutterBottom>
        Usuários
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Usuário</TableCell>
              <TableCell>E-mail</TableCell>
              <TableCell>Departamento</TableCell>
              <TableCell>Cargo</TableCell>
              <TableCell>Função</TableCell>
              <TableCell align="center">Ações</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {users.map((user) => (
              <TableRow key={user.id}>
                <TableCell>
                  <Box display="flex" alignItems="center">
                    <Avatar sx={{ mr: 2 }}>
                      <PersonIcon />
                    </Avatar>
                    {user.displayName}
                  </Box>
                </TableCell>
                <TableCell>{user.email}</TableCell>
                <TableCell>{user.department || "-"}</TableCell>
                <TableCell>{user.jobTitle || "-"}</TableCell>
                <TableCell>
                  <Chip
                    label={getRoleLabel(user.role)}
                    color={getRoleColor(user.role)}
                    size="small"
                  />
                </TableCell>
                <TableCell align="center">
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={() => handleEdit(user)}
                    title="Editar"
                  >
                    <EditIcon />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Dialog de edição */}
      <Dialog 
        open={editDialogOpen} 
        onClose={() => setEditDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Editar Usuário</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 2 }}>
            <TextField
              label="Nome"
              value={editForm.displayName}
              onChange={(e) => setEditForm({ ...editForm, displayName: e.target.value })}
              fullWidth
            />
            <TextField
              label="Departamento"
              value={editForm.department}
              onChange={(e) => setEditForm({ ...editForm, department: e.target.value })}
              fullWidth
            />
            <TextField
              label="Cargo"
              value={editForm.jobTitle}
              onChange={(e) => setEditForm({ ...editForm, jobTitle: e.target.value })}
              fullWidth
            />
            <FormControl fullWidth>
              <InputLabel>Função</InputLabel>
              <Select
                value={editForm.role}
                onChange={(e) => setEditForm({ ...editForm, role: e.target.value as UserRole })}
                label="Função"
              >
                <MenuItem value="Employee">Funcionário</MenuItem>
                <MenuItem value="Manager">Gerente</MenuItem>
                <MenuItem value="Admin">Administrador</MenuItem>
              </Select>
            </FormControl>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialogOpen(false)}>Cancelar</Button>
          <Button onClick={handleSaveEdit} variant="contained">
            Salvar
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default Users; 
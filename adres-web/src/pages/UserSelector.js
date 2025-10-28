import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Star, User, Loader2 } from 'lucide-react';
import { Button } from '../components/Button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { apiService } from '../services/api';

export default function UserSelector() {
  const navigate = useNavigate();
  const [users, setUsers] = useState([]);
  const [selectedUser, setSelectedUser] = useState(null);
  const [loading, setLoading] = useState(false);
  const [loadingUsers, setLoadingUsers] = useState(true);
  const [error, setError] = useState('');

  // Cargar usuarios dinámicamente al montar el componente
  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      setLoadingUsers(true);
      const usersFromDB = await apiService.getAllUsers();
      setUsers(usersFromDB);
      setError('');
    } catch (err) {
      const apiUrl = process.env.REACT_APP_API_BASE_URL || 'http://localhost:8080/api';
      setError(`Error al cargar usuarios. Verifica que la API esté ejecutándose en ${apiUrl}`);
      console.error('Error cargando usuarios:', err);
    } finally {
      setLoadingUsers(false);
    }
  };

  const handleSelectUser = async (user) => {
    setLoading(true);
    setSelectedUser(user);
    setError('');

    try {
      // Generar token mock para el usuario
      const token = apiService.generateMockToken(user);
      
      // Simular delay de autenticación
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      // Guardar token y obtener perfil
      localStorage.setItem('token', token);
      const profile = await apiService.getMe(token);
      localStorage.setItem('user', JSON.stringify(profile));
      
      // Redirigir al dashboard
      navigate('/dashboard');
    } catch (err) {
      setError('Error al autenticar. Verifica que la API esté ejecutándose.');
      setLoading(false);
      setSelectedUser(null);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-blue-50">
      <div className="max-w-4xl mx-auto px-4 py-12">
        {/* Header */}
        <div className="mb-12">
          <Button
            variant="ghost"
            onClick={() => navigate('/')}
            className="mb-6"
          >
            <ArrowLeft className="w-4 h-4" />
            Volver
          </Button>
          
          <div className="text-center">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-blue-600 to-purple-600 rounded-2xl mb-6">
              <User className="w-8 h-8 text-white" />
            </div>
            <h1 className="text-4xl font-bold text-gray-900 mb-3">
              Seleccionar Usuario
            </h1>
            <p className="text-lg text-gray-600">
              Simulador de Autenticación Externa
            </p>
          </div>
        </div>

        {/* Info Card */}
        <Card className="mb-8 border-blue-200 bg-blue-50/50">
          <CardContent className="pt-6">
            <p className="text-sm text-gray-700">
              <strong>Modo Demostración:</strong> Selecciona un perfil para simular la autenticación.
              El sistema cargará usuarios desde la base de datos y generará un token JWT.
            </p>
          </CardContent>
        </Card>

        {/* Loading State */}
        {loadingUsers && (
          <div className="flex flex-col items-center justify-center py-12">
            <Loader2 className="w-12 h-12 text-blue-600 animate-spin mb-4" />
            <p className="text-gray-600">Cargando usuarios desde la base de datos...</p>
          </div>
        )}

        {/* Users Grid */}
        {!loadingUsers && users.length > 0 && (
          <div className="grid md:grid-cols-2 gap-6 mb-8">
            {users.map((user, index) => {
              const avatarColors = [
                'from-purple-500 to-pink-600',
                'from-green-500 to-emerald-600',
                'from-blue-500 to-cyan-600',
                'from-orange-500 to-red-600',
                'from-teal-500 to-blue-600'
              ];
              const colorClass = avatarColors[index % avatarColors.length];
              
              return (
                <Card
                  key={user.id}
                  className={`cursor-pointer transition-all duration-300 hover:shadow-lg ${
                    selectedUser?.id === user.id && loading
                      ? 'ring-2 ring-blue-500 shadow-lg'
                      : 'hover:scale-105'
                  }`}
                  onClick={() => !loading && handleSelectUser(user)}
                >
                  <CardHeader>
                    <div className="flex items-start justify-between mb-4">
                      <div className={`w-16 h-16 rounded-xl flex items-center justify-center text-white font-bold text-2xl bg-gradient-to-br ${colorClass}`}>
                        {(user.fullName || user.name || user.username).substring(0, 2).toUpperCase()}
                      </div>
                      {user.esRepresentanteLegal && (
                        <div className="flex items-center gap-1 px-2 py-1 bg-yellow-100 text-yellow-700 rounded-lg text-xs font-semibold">
                          <Star className="w-3 h-3 fill-current" />
                          RL
                        </div>
                      )}
                    </div>
                    <CardTitle className="text-xl">{user.fullName || user.name || user.username}</CardTitle>
                    <CardDescription className="text-base">{user.email}</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-gray-600 mb-4">
                      {user.roles && user.roles.length > 0 
                        ? `Roles: ${user.roles.join(', ')}`
                        : 'Sin roles asignados'}
                    </p>
                    <div className="flex items-center justify-between">
                      <div className="flex flex-wrap gap-2">
                        {user.roles && user.roles.map((role) => (
                          <span key={role} className="inline-flex items-center px-3 py-1 bg-blue-100 text-blue-700 rounded-full text-xs font-semibold">
                            {role}
                          </span>
                        ))}
                      </div>
                      {selectedUser?.id === user.id && loading ? (
                        <div className="flex items-center gap-2 text-blue-600 text-sm font-medium">
                          <Loader2 className="w-4 h-4 animate-spin" />
                          Autenticando...
                        </div>
                      ) : (
                        <span className="text-sm font-semibold text-blue-600">
                          Seleccionar →
                        </span>
                      )}
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}

        {/* Empty State */}
        {!loadingUsers && users.length === 0 && !error && (
          <Card className="text-center py-12">
            <CardContent>
              <User className="w-16 h-16 text-gray-300 mx-auto mb-4" />
              <p className="text-gray-600 mb-2">No hay usuarios disponibles</p>
              <p className="text-sm text-gray-500">Agrega usuarios a la base de datos para que aparezcan aquí</p>
            </CardContent>
          </Card>
        )}

        {/* Error Message */}
        {error && (
          <Card className="border-red-200 bg-red-50">
            <CardContent className="pt-6">
              <p className="text-sm text-red-700">{error}</p>
            </CardContent>
          </Card>
        )}

        {/* Footer Note */}
        <Card className="border-gray-200">
          <CardContent className="pt-6">
            <p className="text-xs text-gray-600 leading-relaxed">
              <strong>Nota:</strong> Este simulador será reemplazado por tu proveedor de autenticación real
              (OAuth 2.0 / OpenID Connect) en producción. Asegúrate de que el backend esté corriendo en{' '}
              <code className="px-2 py-0.5 bg-gray-100 rounded font-mono">{process.env.REACT_APP_API_BASE_URL || 'http://localhost:8080/api'}</code>
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

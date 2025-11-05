import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { LogOut, FileText, Users, DollarSign, BarChart3, CheckCircle, Clock, AlertCircle, Shield } from 'lucide-react';
import { Button } from '../components/Button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { apiService } from '../services/api';

export default function AdminDashboard() {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [isRL, setIsRL] = useState(false);
  const [rlLoading, setRlLoading] = useState(false);

  useEffect(() => {
    const userData = localStorage.getItem('user');
    if (!userData) {
      navigate('/');
    } else {
      setUser(JSON.parse(userData));
      checkRepresentanteLegal();
    }
  }, [navigate]);

  const checkRepresentanteLegal = async () => {
    try {
      const token = localStorage.getItem('token');
      const result = await apiService.getSoloRepresentanteLegal(token);
      setIsRL(true);
      console.log('✅ Usuario es Representante Legal:', result);
    } catch (error) {
      setIsRL(false);
      console.log('❌ Usuario NO es Representante Legal');
    }
  };

  const handleRLAction = async (actionName) => {
    setRlLoading(true);
    try {
      const token = localStorage.getItem('token');
      const result = await apiService.getSoloRepresentanteLegal(token);
      alert(`✅ ${actionName} ejecutada exitosamente!\n\n${result.message}`);
    } catch (error) {
      alert(`❌ Acceso denegado\n\nSolo los Representantes Legales pueden realizar esta acción.`);
    } finally {
      setRlLoading(false);
    }
  };

  const handleLogout = () => {
    navigate('/auth/logout');
  };

  if (!user) return null;

  const stats = [
    { title: 'Solicitudes Pendientes', value: '24', icon: FileText, color: 'blue', change: '+12%' },
    { title: 'Pagos Procesados', value: '156', icon: DollarSign, color: 'green', change: '+8%' },
    { title: 'Usuarios Activos', value: '89', icon: Users, color: 'purple', change: '+23%' },
    { title: 'Reportes Generados', value: '42', icon: BarChart3, color: 'orange', change: '+15%' },
  ];

  const recentActivities = [
    { text: 'Solicitud #1234 aprobada', time: 'Hace 5 min', status: 'success' },
    { text: 'Nuevo usuario registrado: Ana Torres', time: 'Hace 15 min', status: 'info' },
    { text: 'Pago #5678 requiere revisión', time: 'Hace 1 hora', status: 'warning' },
    { text: 'Reporte mensual generado', time: 'Hace 2 horas', status: 'success' },
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navbar */}
      <nav className="bg-white border-b sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex justify-between items-center">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-blue-600 to-purple-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-xl">A</span>
              </div>
              <div>
                <h1 className="text-xl font-bold text-gray-900">ADRES</h1>
                <p className="text-xs text-gray-500">Panel Administrativo</p>
              </div>
            </div>
            
            <div className="flex items-center gap-4">
              <div className="text-right hidden sm:block">
                <p className="text-sm font-semibold text-gray-900">{user.fullName || user.name || user.username}</p>
                <p className="text-xs text-gray-500">{user.email}</p>
              </div>
              <Button variant="destructive" size="sm" onClick={handleLogout}>
                <LogOut className="w-4 h-4" />
                Salir
              </Button>
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h2 className="text-3xl font-bold text-gray-900 mb-2">
            Bienvenido, {user.fullName || user.name || user.username}
          </h2>
          <p className="text-gray-600">Panel de control administrativo del sistema ADRES</p>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          {stats.map((stat, index) => (
            <Card key={index} className="hover:shadow-md transition-shadow">
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <div className={`w-10 h-10 bg-${stat.color}-100 rounded-lg flex items-center justify-center`}>
                    <stat.icon className={`w-5 h-5 text-${stat.color}-600`} />
                  </div>
                  <span className={`text-xs font-semibold text-${stat.color}-600 bg-${stat.color}-50 px-2 py-1 rounded-full`}>
                    {stat.change}
                  </span>
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-3xl font-bold text-gray-900 mb-1">{stat.value}</div>
                <p className="text-sm text-gray-600">{stat.title}</p>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Representante Legal Banner & Actions */}
        {isRL && (
          <Card className="mb-8 border-purple-200 bg-gradient-to-r from-purple-50 to-pink-50">
            <CardHeader>
              <div className="flex items-center gap-3">
                <div className="w-12 h-12 bg-gradient-to-br from-purple-600 to-pink-600 rounded-xl flex items-center justify-center">
                  <Shield className="w-6 h-6 text-white" />
                </div>
                <div>
                  <CardTitle className="text-purple-900">Representante Legal</CardTitle>
                  <CardDescription className="text-purple-700">
                    Tienes privilegios especiales para acciones de alto nivel
                  </CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <Button
                  variant="outline"
                  className="h-auto py-4 flex-col gap-2 border-purple-300 hover:bg-purple-100"
                  onClick={() => handleRLAction('Aprobar Solicitud Especial')}
                  disabled={rlLoading}
                >
                  <CheckCircle className="w-5 h-5 text-purple-600" />
                  <span className="text-sm font-semibold">Aprobar Solicitudes Especiales</span>
                </Button>
                <Button
                  variant="outline"
                  className="h-auto py-4 flex-col gap-2 border-purple-300 hover:bg-purple-100"
                  onClick={() => handleRLAction('Firmar Documento Oficial')}
                  disabled={rlLoading}
                >
                  <FileText className="w-5 h-5 text-purple-600" />
                  <span className="text-sm font-semibold">Firmar Documentos Oficiales</span>
                </Button>
                <Button
                  variant="outline"
                  className="h-auto py-4 flex-col gap-2 border-purple-300 hover:bg-purple-100"
                  onClick={() => handleRLAction('Autorizar Pago Mayor')}
                  disabled={rlLoading}
                >
                  <DollarSign className="w-5 h-5 text-purple-600" />
                  <span className="text-sm font-semibold">Autorizar Pagos Mayores</span>
                </Button>
                <Button
                  variant="outline"
                  className="h-auto py-4 flex-col gap-2 border-purple-300 hover:bg-purple-100"
                  onClick={() => handleRLAction('Gestionar Usuario del Sistema')}
                  disabled={rlLoading}
                >
                  <Users className="w-5 h-5 text-purple-600" />
                  <span className="text-sm font-semibold">Gestionar Usuarios del Sistema</span>
                </Button>
              </div>
            </CardContent>
          </Card>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Quick Actions */}
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle>Acciones Rápidas</CardTitle>
              <CardDescription>Operaciones frecuentes del sistema</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4">
                <Button variant="outline" className="h-auto py-6 flex-col gap-2">
                  <FileText className="w-6 h-6" />
                  <span>Nueva Solicitud</span>
                </Button>
                <Button variant="outline" className="h-auto py-6 flex-col gap-2">
                  <CheckCircle className="w-6 h-6" />
                  <span>Aprobar Pagos</span>
                </Button>
                <Button variant="outline" className="h-auto py-6 flex-col gap-2">
                  <Users className="w-6 h-6" />
                  <span>Gestionar Usuarios</span>
                </Button>
                <Button variant="outline" className="h-auto py-6 flex-col gap-2">
                  <BarChart3 className="w-6 h-6" />
                  <span>Ver Reportes</span>
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Recent Activity */}
          <Card>
            <CardHeader>
              <CardTitle>Actividad Reciente</CardTitle>
              <CardDescription>Últimas acciones del sistema</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {recentActivities.map((activity, index) => (
                  <div key={index} className="flex items-start gap-3">
                    <div className={`w-2 h-2 mt-2 rounded-full flex-shrink-0 ${
                      activity.status === 'success' ? 'bg-green-500' :
                      activity.status === 'warning' ? 'bg-yellow-500' :
                      'bg-blue-500'
                    }`} />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-900 truncate">{activity.text}</p>
                      <p className="text-xs text-gray-500">{activity.time}</p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Roles & Permissions */}
        <Card className="mt-6">
          <CardHeader>
            <CardTitle>Tu Perfil de Acceso</CardTitle>
            <CardDescription>Roles y permisos asignados a tu cuenta</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid md:grid-cols-2 gap-6">
              <div>
                <h4 className="text-sm font-semibold text-gray-700 mb-3">Roles ({user.roles.length})</h4>
                <div className="flex flex-wrap gap-2">
                  {user.roles.map((role) => (
                    <span key={role} className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-blue-100 text-blue-700 text-sm font-semibold rounded-lg">
                      {role}
                    </span>
                  ))}
                </div>
              </div>
              <div>
                <h4 className="text-sm font-semibold text-gray-700 mb-3">Permisos ({user.permissions.length})</h4>
                <div className="flex flex-wrap gap-2">
                  {user.permissions.map((permission) => (
                    <span key={permission} className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-green-100 text-green-700 text-sm font-semibold rounded-lg">
                      {permission.replace('_', ' ')}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </main>
    </div>
  );
}

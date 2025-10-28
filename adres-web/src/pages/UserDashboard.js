import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { LogOut, FileText, DollarSign, File, BarChart3, CheckCircle, Clock, AlertTriangle, Shield } from 'lucide-react';
import { Button } from '../components/Button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card';
import { apiService } from '../services/api';

export default function UserDashboard() {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [rlLoading, setRlLoading] = useState(false);

  useEffect(() => {
    const userData = localStorage.getItem('user');
    if (!userData) {
      navigate('/');
    } else {
      setUser(JSON.parse(userData));
    }
  }, [navigate]);

  const handleTryRLAccess = async () => {
    setRlLoading(true);
    try {
      const token = localStorage.getItem('token');
      const result = await apiService.getSoloRepresentanteLegal(token);
      alert(`✅ Acceso permitido!\n\n${result.message}`);
    } catch (error) {
      alert(`❌ Acceso denegado\n\nSolo los Representantes Legales pueden realizar esta acción.\n\nEsta es una función restringida que requiere privilegios especiales.`);
    } finally {
      setRlLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    navigate('/');
  };

  if (!user) return null;

  const sections = [
    { title: 'Mis Solicitudes', count: '8', icon: FileText, color: 'blue', description: 'Ver el estado de tus solicitudes' },
    { title: 'Consultar Pagos', count: '24', icon: DollarSign, color: 'green', description: 'Historial de pagos y transacciones' },
    { title: 'Documentos', count: '12', icon: File, color: 'purple', description: 'Acceder a documentos y certificados' },
    { title: 'Reportes', count: '5', icon: BarChart3, color: 'orange', description: 'Generar reportes de actividad' },
  ];

  const recentItems = [
    { title: 'Solicitud #789 - En Revisión', date: '10 Oct 2025', status: 'pending' },
    { title: 'Pago procesado - $1,500,000', date: '08 Oct 2025', status: 'success' },
    { title: 'Documento generado', date: '05 Oct 2025', status: 'success' },
    { title: 'Solicitud #756 - Aprobada', date: '01 Oct 2025', status: 'success' },
  ];

  return (
    <div className="min-h-screen bg-gradient-to-br from-teal-50 to-blue-50">
      {/* Navbar */}
      <nav className="bg-white border-b sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex justify-between items-center">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-teal-500 to-emerald-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-xl">A</span>
              </div>
              <div>
                <h1 className="text-xl font-bold text-gray-900">ADRES</h1>
                <p className="text-xs text-gray-500">Portal de Consultas</p>
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
            Hola, {user.fullName || user.name || user.username}
          </h2>
          <p className="text-gray-600">Bienvenido a tu portal de consultas ADRES</p>
        </div>

        {/* Welcome Banner */}
        <Card className="mb-8 bg-gradient-to-r from-teal-500 to-emerald-500 border-0 text-white">
          <CardContent className="pt-6">
            <h3 className="text-2xl font-bold mb-2">Panel de Consultas</h3>
            <p className="text-teal-50">
              Accede a la información de tus solicitudes, pagos y documentos del sistema de salud
            </p>
          </CardContent>
        </Card>

        {/* Representante Legal Warning */}
        <Card className="mb-8 border-yellow-300 bg-yellow-50">
          <CardHeader>
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 bg-yellow-400 rounded-xl flex items-center justify-center">
                <AlertTriangle className="w-6 h-6 text-yellow-900" />
              </div>
              <div>
                <CardTitle className="text-yellow-900">Acceso Limitado</CardTitle>
                <CardDescription className="text-yellow-800">
                  No tienes privilegios de Representante Legal
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-yellow-900 mb-4">
              Algunas funciones avanzadas del sistema están restringidas solo para usuarios con el rol de Representante Legal.
              Si crees que necesitas estos privilegios, contacta con el administrador del sistema.
            </p>
            <Button
              variant="outline"
              className="border-yellow-400 hover:bg-yellow-100 text-yellow-900"
              onClick={handleTryRLAccess}
              disabled={rlLoading}
            >
              <Shield className="w-4 h-4 mr-2" />
              {rlLoading ? 'Verificando...' : 'Intentar Acceso a Función RL'}
            </Button>
          </CardContent>
        </Card>

        {/* Sections Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          {sections.map((section, index) => (
            <Card key={index} className="hover:shadow-lg transition-all duration-300 cursor-pointer group">
              <CardHeader>
                <div className="flex items-center justify-between mb-4">
                  <div className={`w-12 h-12 bg-${section.color}-100 rounded-xl flex items-center justify-center group-hover:scale-110 transition-transform`}>
                    <section.icon className={`w-6 h-6 text-${section.color}-600`} />
                  </div>
                  <span className="text-3xl font-bold text-gray-900">{section.count}</span>
                </div>
                <CardTitle className="text-lg">{section.title}</CardTitle>
                <CardDescription>{section.description}</CardDescription>
              </CardHeader>
              <CardContent>
                <Button variant="ghost" size="sm" className="w-full group-hover:bg-gray-100">
                  Ver detalles →
                </Button>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Recent Activity */}
          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle>Actividad Reciente</CardTitle>
              <CardDescription>Tus últimas transacciones y solicitudes</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {recentItems.map((item, index) => (
                  <div key={index} className="flex items-center gap-4 p-4 rounded-lg border hover:bg-gray-50 transition-colors">
                    <div className={`w-10 h-10 rounded-full flex items-center justify-center flex-shrink-0 ${
                      item.status === 'success' ? 'bg-green-100' :
                      item.status === 'pending' ? 'bg-yellow-100' :
                      'bg-gray-100'
                    }`}>
                      {item.status === 'success' ? (
                        <CheckCircle className="w-5 h-5 text-green-600" />
                      ) : (
                        <Clock className="w-5 h-5 text-yellow-600" />
                      )}
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-semibold text-gray-900">{item.title}</p>
                      <p className="text-xs text-gray-500">{item.date}</p>
                    </div>
                    <Button variant="ghost" size="sm">Ver</Button>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* User Profile */}
          <Card>
            <CardHeader>
              <CardTitle>Tu Perfil</CardTitle>
              <CardDescription>Información de tu cuenta</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Usuario</label>
                <p className="text-sm font-medium text-gray-900 mt-1">{user.fullName || user.name || user.username}</p>
              </div>
              <div>
                <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Correo</label>
                <p className="text-sm font-medium text-gray-900 mt-1">{user.email}</p>
              </div>
              <div>
                <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide">ID</label>
                <p className="text-xs font-mono text-gray-700 mt-1">{user.sub}</p>
              </div>
              <div>
                <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2 block">Roles</label>
                <div className="flex flex-wrap gap-2">
                  {user.roles.map((role) => (
                    <span key={role} className="inline-flex px-2 py-1 bg-teal-100 text-teal-700 text-xs font-semibold rounded">
                      {role}
                    </span>
                  ))}
                </div>
              </div>
              <div>
                <label className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2 block">Permisos</label>
                <div className="flex flex-wrap gap-2">
                  {user.permissions.map((permission) => (
                    <span key={permission} className="inline-flex px-2 py-1 bg-green-100 text-green-700 text-xs font-semibold rounded">
                      {permission.replace('_', ' ')}
                    </span>
                  ))}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  );
}

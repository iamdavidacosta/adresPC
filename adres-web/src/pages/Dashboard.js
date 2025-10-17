import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import AdminDashboard from './AdminDashboard';
import UserDashboard from './UserDashboard';

export default function Dashboard() {
  const navigate = useNavigate();
  const [user, setUser] = useState(null);

  useEffect(() => {
    const userData = localStorage.getItem('user');
    if (!userData) {
      navigate('/');
    } else {
      setUser(JSON.parse(userData));
    }
  }, [navigate]);

  if (!user) return null;

  // Determinar si es Admin basado en roles
  const isAdmin = user.roles.some(role => 
    role === 'Admin' || role === 'Analista'
  );

  return isAdmin ? <AdminDashboard /> : <UserDashboard />;
}

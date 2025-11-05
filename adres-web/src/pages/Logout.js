import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:8080/api';

function Logout() {
  const navigate = useNavigate();

  useEffect(() => {
    const performLogout = async () => {
      try {
        const accessToken = localStorage.getItem('access_token');
        const idToken = localStorage.getItem('id_token');

        // 1. Revocar el token en el backend
        if (accessToken) {
          try {
            await fetch(`${API_BASE_URL}/AdresAuth/logout`, {
              method: 'POST',
              headers: {
                'Authorization': `Bearer ${accessToken}`,
                'Content-Type': 'application/json',
              },
            });
          } catch (error) {
            console.error('Error revocando token:', error);
          }
        }

        // 2. Limpiar localStorage
        localStorage.removeItem('access_token');
        localStorage.removeItem('id_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('user');

        // 3. Obtener la URL de logout del IdP
        const logoutUrlResponse = await fetch(
          `${API_BASE_URL}/AdresAuth/logout-url?` +
          `id_token_hint=${encodeURIComponent(idToken || '')}&` +
          `post_logout_redirect_uri=${encodeURIComponent(window.location.origin)}`
        );

        if (logoutUrlResponse.ok) {
          const data = await logoutUrlResponse.json();
          
          console.log('🔓 Redirigiendo a logout del IdP:', data.logout_url);
          
          // 4. Redirigir al endpoint de logout del IdP
          window.location.href = data.logout_url;
        } else {
          // Si falla, redirigir al home
          navigate('/');
        }
      } catch (error) {
        console.error('❌ Error en logout:', error);
        // En caso de error, limpiar y redirigir al home
        localStorage.clear();
        navigate('/');
      }
    };

    performLogout();
  }, [navigate]);

  return (
    <div style={{ 
      display: 'flex', 
      justifyContent: 'center', 
      alignItems: 'center', 
      height: '100vh',
      flexDirection: 'column',
      gap: '20px'
    }}>
      <div className="spinner-border text-primary" role="status">
        <span className="visually-hidden">Cerrando sesión...</span>
      </div>
      <p>Cerrando sesión...</p>
    </div>
  );
}

export default Logout;

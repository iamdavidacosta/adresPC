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

        // 3. Construir la URL de logout del IdP directamente
        const endSessionEndpoint = 'https://idp.autenticsign.com/connect/endsession';
        const postLogoutRedirectUri = window.location.origin; // https://adres-autenticacion.centralspike.com
        
        const logoutParams = new URLSearchParams();
        if (idToken) {
          logoutParams.append('id_token_hint', idToken);
        }
        logoutParams.append('post_logout_redirect_uri', postLogoutRedirectUri);
        
        const logoutUrl = `${endSessionEndpoint}?${logoutParams.toString()}`;
        
        console.log('🔓 Redirigiendo a logout del IdP:', logoutUrl);
        console.log('📍 Volverá a:', postLogoutRedirectUri);
        
        // 4. Redirigir al endpoint de logout del IdP
        window.location.href = logoutUrl;
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

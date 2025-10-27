import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export default function AuthCallback() {
  const navigate = useNavigate();
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // Prevenir múltiples ejecuciones
        const processedKey = 'oauth_callback_processed';
        if (sessionStorage.getItem(processedKey)) {
          console.log('Callback ya procesado, redirigiendo...');
          navigate('/dashboard');
          return;
        }

        // Obtener parámetros de la URL
        const params = new URLSearchParams(window.location.search);
        const code = params.get('code');
        const state = params.get('state');
        const errorParam = params.get('error');
        const errorDescription = params.get('error_description');

        // Verificar si hay error en la respuesta de OAuth
        if (errorParam) {
          throw new Error(errorDescription || errorParam);
        }

        if (!code || !state) {
          throw new Error('Código o state faltante en la respuesta de autenticación');
        }

        // Decodificar el state para obtener el code_verifier
        const stateData = JSON.parse(atob(state));
        const { cv: codeVerifier, returnUrl } = stateData;

        if (!codeVerifier) {
          throw new Error('Code verifier no encontrado en el state');
        }

        console.log('🔄 Intercambiando código por tokens...');
        console.log('Code:', code.substring(0, 10) + '...');
        console.log('Code Verifier:', codeVerifier.substring(0, 10) + '...');

        // Intercambiar el código por tokens
        const tokenResponse = await fetch('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/token', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            code,
            codeVerifier,
            redirectUri: 'https://adres-autenticacion.centralspike.com/auth/callback'
          })
        });

        if (!tokenResponse.ok) {
          const errorData = await tokenResponse.json();
          console.error('❌ Error del servidor:', errorData);
          
          // Si el código ya fue usado o expiró
          if (errorData.error === 'invalid_grant') {
            throw new Error('El código de autenticación ya fue usado o expiró. Por favor, inicia sesión nuevamente.');
          }
          throw new Error(errorData.message || 'Error al intercambiar el código por tokens');
        }

        const tokenData = await tokenResponse.json();

        console.log('✅ Tokens recibidos:', {
          access_token: tokenData.access_token?.substring(0, 20) + '...',
          token_type: tokenData.token_type,
          expires_in: tokenData.expires_in
        });

        // Guardar tokens en localStorage (usando snake_case que viene del backend)
        if (tokenData.access_token) {
          localStorage.setItem('access_token', tokenData.access_token);
        } else {
          throw new Error('No se recibió access_token del servidor');
        }
        
        if (tokenData.refresh_token) {
          localStorage.setItem('refresh_token', tokenData.refresh_token);
        }
        if (tokenData.id_token) {
          localStorage.setItem('id_token', tokenData.id_token);
        }

        // Obtener información del usuario
        const userResponse = await fetch('https://adres-autenticacion-back.centralspike.com/api/AdresAuth/me', {
          headers: {
            'Authorization': `Bearer ${tokenData.access_token}`
          }
        });

        if (!userResponse.ok) {
          throw new Error('Error al obtener información del usuario');
        }

        const userData = await userResponse.json();

        // Guardar información del usuario en localStorage
        localStorage.setItem('user', JSON.stringify(userData));
        
        // Marcar callback como procesado
        sessionStorage.setItem(processedKey, 'true');
        
        console.log('✅ Autenticación completada exitosamente');

        // Redirigir según el rol del usuario
        if (userData.role === 'admin' || userData.role === 'Admin') {
          navigate('/admin/dashboard');
        } else if (returnUrl && returnUrl !== '/') {
          navigate(returnUrl);
        } else {
          navigate('/dashboard');
        }

      } catch (err) {
        console.error('❌ Error en callback de autenticación:', err);
        setError(err.message);
        setLoading(false);
      }
    };

    handleCallback();
  }, [navigate]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-16 w-16 border-b-2 border-blue-600 mb-4"></div>
          <h2 className="text-2xl font-bold text-gray-900 mb-2">Autenticando...</h2>
          <p className="text-gray-600">Por favor espera mientras verificamos tu identidad</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center">
        <div className="max-w-md w-full mx-4">
          <div className="bg-white rounded-2xl shadow-lg p-8 border border-red-100">
            <div className="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mb-4 mx-auto">
              <svg className="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2 text-center">Error de Autenticación</h2>
            <p className="text-gray-600 text-center mb-6">{error}</p>
            <div className="space-y-3">
              <button
                onClick={() => {
                  sessionStorage.clear();
                  window.location.href = 'https://adres-autenticacion-back.centralspike.com/api/AdresAuth/authorize';
                }}
                className="w-full bg-gradient-to-r from-blue-600 to-purple-600 text-white py-3 px-6 rounded-lg font-medium hover:shadow-lg transition-shadow"
              >
                Intentar de Nuevo
              </button>
              <button
                onClick={() => {
                  sessionStorage.clear();
                  window.location.href = '/';
                }}
                className="w-full bg-gray-100 text-gray-700 py-3 px-6 rounded-lg font-medium hover:bg-gray-200 transition-colors"
              >
                Volver al Inicio
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return null;
}

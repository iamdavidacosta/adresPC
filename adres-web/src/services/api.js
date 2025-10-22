const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:8080/api';

export const apiService = {
  async getMe(token) {
    const response = await fetch(`${API_BASE_URL}/Me`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error('Error al obtener perfil de usuario');
    }

    return response.json();
  },

  async getSoloRepresentanteLegal(token) {
    const response = await fetch(`${API_BASE_URL}/Secure/solo-rl`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error('Error al verificar acceso');
    }

    return response.json();
  },

  // Nuevo método para obtener todos los usuarios dinámicamente
  async getAllUsers() {
    const response = await fetch(`${API_BASE_URL}/Users`, {
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error('Error al obtener usuarios');
    }

    return response.json();
  },

  // Genera un token mock JWT para el usuario seleccionado
  generateMockToken(user) {
    const header = btoa(JSON.stringify({ alg: "none", typ: "JWT" }));
    const payload = btoa(JSON.stringify({
      sub: user.sub,
      email: user.email,
      esRepresentanteLegal: user.esRepresentanteLegal ? "true" : "false",
      exp: 9999999999,
      iat: 1700000000
    }));
    return `${header}.${payload}.`;
  }
};
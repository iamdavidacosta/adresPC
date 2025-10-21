# 📋 Resumen: URLs para Equipo de Autenticación

## 🎯 URLs a Configurar en el Autenticador Externo

### 🟡 **STAGING**

**Callback URLs (Redirect URIs)**:
```
https://app.staging.adres.gov.co/auth/callback
https://api.staging.adres.gov.co/api/Auth/callback
```

**Logout Redirect URL**:
```
https://app.staging.adres.gov.co
```

**Error Redirect URL**:
```
https://app.staging.adres.gov.co/auth/error
```

**CORS Origins**:
```
https://app.staging.adres.gov.co
https://api.staging.adres.gov.co
```

---

### 🔴 **PRODUCCIÓN**

**Callback URLs (Redirect URIs)**:
```
https://app.adres.gov.co/auth/callback
https://api.adres.gov.co/api/Auth/callback
```

**Logout Redirect URL**:
```
https://app.adres.gov.co
```

**Error Redirect URL**:
```
https://app.adres.gov.co/auth/error
```

**CORS Origins**:
```
https://app.adres.gov.co
https://api.adres.gov.co
```

---

## 🔐 Información del Cliente

- **Client ID**: `adres-api`
- **Scopes requeridos**: `openid`, `profile`, `email`, `roles`, `permissions`
- **Grant Type**: `authorization_code`, `refresh_token`
- **Response Type**: `code`

---

## 📄 Claims Requeridos en el Token JWT

```json
{
  "sub": "id-usuario",
  "email": "usuario@ejemplo.com",
  "name": "Nombre Completo",
  "esRepresentanteLegal": "true",
  "roles": ["Admin", "Analista"],
  "permissions": ["CONSULTAR_PAGOS", "CREAR_SOLICITUD"]
}
```

---

## 📖 Documentación Completa

Ver: `AUTHENTICATION_URLS.md` para más detalles

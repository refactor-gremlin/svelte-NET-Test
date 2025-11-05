import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'http://localhost:5000/swagger/v1/swagger.json',
  output: 'api/schema',
  plugins: [
    '@hey-api/client-fetch',               // HTTP client plugin :contentReference[oaicite:3]{index=3}
    {
      name: 'zod',                          // Zod schemas plugin with Zod 4 compatibility
      compatibilityVersion: 4,             // Explicitly use Zod 4
    },
    '@hey-api/schemas',                     // JSON Schema objects (optional) :contentReference[oaicite:5]{index=5}
    {
      name: '@hey-api/sdk',                
      validator: true,                      // Enable Zod-based runtime validation :contentReference[oaicite:6]{index=6}
    },
  ],
  auth: () => localStorage.getItem('authToken') ?? '',
});

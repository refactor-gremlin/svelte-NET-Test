// src/api/config.ts
import { dev } from '$app/environment';
import { PUBLIC_API_ENDPOINT } from '$env/static/public';

const defaultDevUrl  = 'http://localhost:5000';
const defaultDockerDevUrl = 'http://api:5000'; // Default for docker development
const defaultProdUrl = 'http://api:5000'; // Default for docker production

export const config = {
  apiEndpoint:
    // 1) if someone set the .env var, use it
    (PUBLIC_API_ENDPOINT && PUBLIC_API_ENDPOINT !== '')
      ? PUBLIC_API_ENDPOINT
      // 2) otherwise pick based on environment
      : (process.env.NODE_ENV === 'production' 
          ? defaultProdUrl 
          : (process.env.NODE_ENV === 'docker-dev' 
              ? defaultDockerDevUrl 
              : defaultDevUrl))
};

//** Also check openapi-ts.config.ts for the API endpoint, one is for runtime, one is for generation */
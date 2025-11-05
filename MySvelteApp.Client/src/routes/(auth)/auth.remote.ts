import { form, command, query } from '$app/server';
import { getRequestEvent } from '$app/server';
import { error } from '@sveltejs/kit';
import { postAuthLogin, postAuthRegister, getTestAuth, getAuthMe } from '$api/schema/sdk.gen';
import { zAuthErrorResponse } from '$api/schema/zod.gen';
import { z } from 'zod';

// Stricter UI-side validation schemas for immediate feedback
const zLoginForm = z.object({
	username: z.string().trim().min(1, 'Username is required'),
	password: z.string().min(1, 'Password is required'),
});

const zRegisterForm = z.object({
	username: z.string().trim().min(1, 'Username is required'),
	email: z.string().email('Valid email required'),
	password: z.string().min(8, 'Password must be at least 8 characters'),
});

// Login form handler with automatic validation
export const login = form(async (data) => {
	// Validate form data with stricter UI schema
	const validationResult = zLoginForm.safeParse({
		username: data.get('username'),
		password: data.get('password'),
	});
	if (!validationResult.success) {
		error(400, 'Invalid login data');
	}
	
	const loginData = validationResult.data;
	const { cookies } = getRequestEvent();
	
	try {
		// Use generated API client with ThrowOnError for cleaner control flow
		const response = await postAuthLogin({
			body: loginData,
			throwOnError: true as const
		});

		const result = response.data;

		// Set JWT token in cookie
		if (result?.token) {
			cookies.set('auth_token', result.token, {
				path: '/',
				httpOnly: true,
				secure: import.meta.env.PROD,
				sameSite: 'strict'
			});
		}
		
		return result;
	} catch (err) {
		console.error('Login error:', err);
		const parsed = zAuthErrorResponse.safeParse(err);
		const message = parsed.success && parsed.data.message
			? parsed.data.message
			: (err instanceof Error ? err.message : 'Network error. Please check your connection and try again.');
		error(401, message);
	}
});

// Registration form handler with automatic validation
export const register = form(async (data) => {

	// Validate passwords match
	const confirmPassword = data.get('confirmPassword');
	if (!confirmPassword || confirmPassword !== data.get('password')) {
		error(400, 'Passwords do not match');
	}

	// Validate form data with stricter UI schema
	const validationResult = zRegisterForm.safeParse({
		username: data.get('username'),
		email: data.get('email'),
		password: data.get('password'),
	});
	if (!validationResult.success) {
		error(400, 'Invalid registration data');
	}
	
	const registerData = validationResult.data;
	
	try {
		// Use generated API client with ThrowOnError
		const response = await postAuthRegister({
			body: registerData,
			throwOnError: true as const
		});

		const result = response.data;

		return result;
	} catch (err) {
		console.log('Registration catch error:', err);
		const parsed = zAuthErrorResponse.safeParse(err);
		const message = parsed.success && parsed.data.message
			? parsed.data.message
			: (err instanceof Error ? err.message : 'Registration failed');
		error(400, message);
	}
});

// Logout command
export const logout = command(async () => {
	const { cookies } = getRequestEvent();
	
	// Clear auth token cookie
	cookies.delete('auth_token', { path: '/' });
	
	return { success: true };
});

// Get current user query
export const getCurrentUser = query(async () => {
	const { cookies } = getRequestEvent();

	const token = cookies.get('auth_token');
	if (!token) {
		error(401, 'Not authenticated');
	}

	// Get current user from backend using the new getAuthMe endpoint
	try {
		const response = await getAuthMe({
			headers: {
				'Authorization': `Bearer ${token}`
			},
			throwOnError: true as const
		});

		const result = response.data;
		
		// Return the actual user data from the backend
		return {
			id: result.user?.id?.toString() || '',
			email: result.user?.email || '',
			name: result.user?.username || '',
			token
		};
	} catch (err) {
		console.error('Get current user error:', err);
		const parsed = zAuthErrorResponse.safeParse(err);
		const message = parsed.success && parsed.data.message
			? parsed.data.message
			: 'Authentication failed';
		error(401, message);
	}
});

// Check if user is authenticated
export const isAuthenticated = query(async () => {
	const { cookies } = getRequestEvent();
	const token = cookies.get('auth_token');
	return !!token;
});
<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import { getRandomPokemonData } from './data.remote';
	import { Card, CardContent } from '$lib/components/ui/card';
	import { Skeleton } from '$lib/components/ui/skeleton';
</script>

<div
	class="min-h-screen px-4 py-12 sm:px-6 lg:px-8"
	>
	<div class="mx-auto max-w-7xl">
		<div class="mb-8 text-center">
			<Button onclick={() => getRandomPokemonData().refresh()}>Click me to see a random pokemon</Button>
		</div>
		
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
			<svelte:boundary>
				{#await getRandomPokemonData()}
					<div class="col-span-full flex items-center justify-center py-12">
						<Card class="h-fit w-full max-w-md">
							<CardContent>
								<div class="flex justify-center">
									<Skeleton class="h-32 w-32 rounded-full" />
								</div>
								<Skeleton class="h-4 w-full my-2" />
								<Skeleton class="h-4 w-full my-2" />
							</CardContent>
						</Card>
					</div>
				{:then pokemon}
					<div class="col-span-full flex justify-center">
						<Card class="h-fit w-full max-w-md">
							<CardContent class="p-6">
								<div class="space-y-4">
									<div class="text-lg font-semibold text-center">Pokemon: {pokemon?.name}</div>
									<div class="flex justify-center">
										<img src={pokemon?.image} alt={pokemon?.name} class="h-32 w-32 object-contain" />
									</div>
									<div class="text-sm text-muted-foreground text-center">Type: {pokemon?.type}</div>
								</div>
							</CardContent>
						</Card>
					</div>
					{:catch error}
					<div class="col-span-full flex items-center justify-center py-12">
						<div class="text-red-500">{error.message}</div>
						<Button onclick={() => getRandomPokemonData().refresh()}>Retry</Button>
					</div>
				{/await}
			</svelte:boundary>
		</div>
	</div>
</div>

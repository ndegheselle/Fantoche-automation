export async function GET({ params, request }) {
    return new Response(
        JSON.stringify([
            {
                scope: "test.name",
                name: "action1",
                inputs: [],
                outputs: [],
            }
        ])
    );
}
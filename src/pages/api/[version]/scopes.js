export async function GET({ params, request }) {
    return new Response(
        JSON.stringify([
            {
                id: "root",
                name: null,
                children: [
                    {
                        name: "action1",
                        inputs: [],
                        outputs: [],
                    }
                ]
            }
        ])
    );
}
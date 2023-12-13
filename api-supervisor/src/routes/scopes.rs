use rocket_contrib::json::Json;
use crate::types::{Result};

#[get("/")]
fn get_all() -> Result<Json<Vec<Scope>>> {
    "Hello, world!"
}

pub fn get_routes() -> Vec<Route>
{
    routes![
        self::scopes::get_all
    ]
}
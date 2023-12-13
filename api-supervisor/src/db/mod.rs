use mongodb::options::ClientOptions;
use mongodb::{Client, Database};
use rocket::fairing::AdHoc;

pub fn init() -> AdHoc {
    // |rocket| is a closure (lambda + parent environment) with a rocket arg
    AdHoc::on_ignite("Connecting to MongoDB", |rocket| async {
        // match can serve as a switch
        // enum Result<T, E> { Ok(T), Err(E) } generic enum with variant (constructors to the Result type)
        // So basically if Result is Ok handle the generic T, else Err ...
        match connect().await {
            Ok(database) => rocket.manage(database),
            Err(error) => {
                // panic! is an Exception, Err would have called it anyway but there we wrap the error for more clarity
                // :? will call the Debug trait to get the error details
                panic!("Cannot connect to instance:: {:?}", error)
            }
        }
    })
}

async fn connect() -> mongodb::error::Result<Database> {
    let mongo_uri = env::var("MONGO_URI").expect("MONGO_URI is not found.");
    let mongo_db_name = env::var("MONGO_DB_NAME").expect("MONGO_DB_NAME is not found.");

    // ? ties the Result of the method to the current scope (will return imediatly if Err or set current value for Ok and continue)
    let client_options = ClientOptions::parse(mongo_uri).await?;
    let client = Client::with_options(client_options)?;
    let database = client.database(mongo_db_name.as_str());

    println!("MongoDB Connected!");

    Ok(database)
}
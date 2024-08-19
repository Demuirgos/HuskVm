include["number.il" as NumberLib, "typedef.ilt" as TypesLib];

Main: {
	let global sum : Number[4] = new Number|[23, 7, 1999, 23071999];
	let global test : Number[1][4] = new Number[4]|[sum];
	let value : Logic[3] = new Logic|[false, false, false];

	let index : Number = 0;
	while(index < 3) do {
		value[index] <-  NumberLib::IsPrime(sum[index]);
		index <- index + 1;
	}

	let result := new TypesLib::Result|{
		IsOk = true,
		Value = value,
		Err = null,
	};

	return result;
};
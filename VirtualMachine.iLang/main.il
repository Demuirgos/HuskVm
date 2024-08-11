include["number.il" as number];

IsPrime : [n] => {
    if (n < 3) then {
        return false;
    } else {
        var i <- 2;
        if ((n % 2) = 0) then {
			return n = 2;
		} else {
            i <- 3;
            while ((i * i) < n) do {
                if((n % i) = 0) then {
                    return false;
                } else {
                    i <- i + 2;
                }
            }
            return true;
        }
    }
};

Main: IsPrime(1983685757);